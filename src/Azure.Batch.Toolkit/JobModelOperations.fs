namespace Batch.Toolkit
open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.Azure.Batch
open Microsoft.Azure.Batch.FileStaging

[<AutoOpen>]
module TaskOperations =
    let GetDefaultTaskSpecification taskName = {
        TaskAffinityInformation = None
        TaskConstraints = None
        TaskCustomBehaviors = []
        TaskDisplayName = None
        TaskEnvironmentSettings = []
        TaskFilesToStage = []
        TaskId = taskName
        //TaskMultiInstanceSettings = None
        TaskResourceFiles = []
        TaskRunElevated = false

        TaskCommandSet = CommandSet.Zero
        TaskArguments = TaskArguments.Zero
        TaskLocalFiles = LocalFiles.Zero
    }

    type internal PreparedTaskSpecification = {
        PreparedTaskAffinityInformation : AffinityInformation option
        PreparedTaskCommandLine : string
        PreparedTaskConstraints : TaskConstraints option
        PreparedTaskCustomBehaviors : BatchClientBehavior list
        PreparedTaskDisplayName : string option
        PreparedTaskEnvironmentSettings : EnvironmentSetting list
        PreparedTaskFilesToStage : IFileStagingProvider list
        PreparedTaskId : TaskName
        //PreparedTaskMultiInstanceSettings : MultiInstanceSettings option
        PreparedTaskResourceFiles : ResourceFile seq
        PreparedTaskRunElevated : bool
    }

    module internal CloudTaskTransforms = 
        let internal toCloudTask task = 
            let (TaskName taskName) = task.PreparedTaskId
            let result = new CloudTask(taskName, task.PreparedTaskCommandLine)

            result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
            result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
            result.AffinityInformation <- task.PreparedTaskAffinityInformation |> getOrNull
            result.Constraints <- task.PreparedTaskConstraints |> getOrNull
            result.DisplayName <- task.PreparedTaskDisplayName |> getOrNull
            result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
            result.CustomBehaviors <- task.PreparedTaskCustomBehaviors |> List<BatchClientBehavior>        
            result

        let internal toJobPreparationTask task = 
            let (TaskName taskName) = task.PreparedTaskId
            let result = new JobPreparationTask ()

            result.Id <- taskName
            result.CommandLine <- task.PreparedTaskCommandLine
            result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
            result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
            result.Constraints <- task.PreparedTaskConstraints |> getOrNull
            result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
            result

        let internal toJobManagerTask task = 
            let (TaskName taskName) = task.PreparedTaskId
            let result = new JobManagerTask ()

            result.Id <- taskName
            result.CommandLine <- task.PreparedTaskCommandLine
            result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
            result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
            result.Constraints <- task.PreparedTaskConstraints |> getOrNull
            result.DisplayName <- task.PreparedTaskDisplayName |> getOrNull
            result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
            result

        let internal toJobReleaseTask task = 
            let (TaskName taskName) = task.PreparedTaskId
            let result = new JobReleaseTask ()

            result.Id <- taskName
            result.CommandLine <- task.PreparedTaskCommandLine
            result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
            result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
            result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
            result

        let internal toPoolStartTask task =
            let result = new StartTask ()

            result.CommandLine <- task.PreparedTaskCommandLine
            result.ResourceFiles <- task.PreparedTaskResourceFiles |> List<ResourceFile>
            result.RunElevated <- task.PreparedTaskRunElevated |> Nullable
            result.EnvironmentSettings <- task.PreparedTaskEnvironmentSettings |> List<EnvironmentSetting>
            result.MaxTaskRetryCount <- Nullable(3)
            result

    let internal bindCommandSet args commandSet = 
        let bindParametersToCommand args pc =
            let interpolateParameter (s : string) (paramName, paramValue) = 
                let placeHolder = sprintf "%%%s%%" paramName
                let value = sprintf "%A" paramValue
                s.Replace(placeHolder, value)

            let lookupParameter (args : Map<string, _>) p =
                (p, args.[p])

            let replaceParameterInString p s = 
                Success p <!> lookupParameter args <!> interpolateParameter s

            pc.Parameters |> foldrM replaceParameterInString pc.Command

        let bindArgument (args : Map<string, _>) = function
        | ParametrizedCommand pc -> pc |> bindParametersToCommand args 
        | SimpleCommand sc -> Success sc 

        let generateCall args c = bindArgument args c <!> sprintf "CALL %s"

        let generateTryCatch args mutate (idx, tc) state =
            let successLabel = sprintf "_SUCCESS_%d" idx
            let errorLabel = sprintf "_ERROR_%d" idx
            succeed {
                let! state = tc.Try |> generateCall args                                          <!> mutate state
                do sprintf "IF NOT %%ERRORLEVEL%% == 0 GOTO %s" errorLabel                         |> mutate state |> ignore
                do sprintf "GOTO %s" successLabel                                                  |> mutate state |> ignore
                do sprintf ":%s" errorLabel                                                        |> mutate state |> ignore
                let! state = tc.Catch |> Option.map (generateCall args) |> getOrElse (Success "") <!> mutate state
                do sprintf ":%s" successLabel                                                      |> mutate state |> ignore
                return state
            }

        let generateFinally args mutate f state = f |> generateCall args <!> mutate state

        let accumulate (buf : StringBuilder) item = buf.AppendLine item
        let buffer = new StringBuilder()

        succeed {
            let! state = commandSet.TryCatchCommands |> List.mapi (fun i c -> (i, c)) |> foldrM (generateTryCatch args accumulate) buffer
            do sprintf ":_FINALLY_" |> accumulate state |> ignore
            let! state = commandSet.FinallyCommands  |> foldrM (generateFinally args accumulate) state
            do sprintf ":_EXIT_"    |> accumulate state |> ignore  
            return state.ToString ()
        }

    let internal defaultPreparedTaskSpecification taskName = {
        PreparedTaskAffinityInformation = None
        PreparedTaskCommandLine = String.Empty
        PreparedTaskConstraints = None
        PreparedTaskCustomBehaviors = []
        PreparedTaskDisplayName = None
        PreparedTaskEnvironmentSettings = []
        PreparedTaskFilesToStage = []
        PreparedTaskId = taskName
        //PreparedTaskMultiInstanceSettings = None
        PreparedTaskResourceFiles = []
        PreparedTaskRunElevated = false
    }

    let internal toPreparedTaskSpecification task taskCommandLine taskResourceFiles = {
        PreparedTaskAffinityInformation = task.TaskAffinityInformation
        PreparedTaskCommandLine = taskCommandLine
        PreparedTaskConstraints = task.TaskConstraints
        PreparedTaskCustomBehaviors = task.TaskCustomBehaviors
        PreparedTaskDisplayName = task.TaskDisplayName
        PreparedTaskEnvironmentSettings = task.TaskEnvironmentSettings
        PreparedTaskFilesToStage = task.TaskFilesToStage
        PreparedTaskId = task.TaskId
        //PreparedTaskMultiInstanceSettings = None
        PreparedTaskResourceFiles = taskResourceFiles
        PreparedTaskRunElevated = task.TaskRunElevated
    }

    let internal prepareTaskForSubmission fileUploader task =
        let ensureCommandLine task =
            let (TaskName taskName) = task.TaskId
            let (TaskArguments args) = task.TaskArguments
            let writeToFile fileName (text : string) = 
                use writer = new StreamWriter(fileName, false, System.Text.Encoding.Unicode)
                text |> writer.Write 
                writer.Flush ()
                writer.Close ()

            succeed {
                let commandScriptFile = sprintf "%s.cmd" taskName |> FileInfo
                let commandLine = sprintf "cmd /c %s" commandScriptFile.Name
                let! commandScript = bindCommandSet args task.TaskCommandSet 
                do writeToFile commandScriptFile.FullName commandScript
                return (commandLine, Some commandScriptFile)
            }

        let ensureUploadedFiles fileUploader (files, script) =
            script |> Option.map (fun s -> LocalFiles [s] + files) |> getOrElse files |> fileUploader 

        succeed {
            let! (taskCommandLine, commandScriptFile) = ensureCommandLine task
            let uploadedTaskFiles = ensureUploadedFiles fileUploader (task.TaskLocalFiles, commandScriptFile)
            return toPreparedTaskSpecification task taskCommandLine uploadedTaskFiles
        } |> extract
            
    let internal submitTask (batchClient : BatchClient) (jobId : string) (task : CloudTask) = 
        batchClient.JobOperations.AddTaskAsync (jobId, task, null, null)

[<AutoOpen>]
module JobOperations =
    let GetDefaultJobSpecification jobName = {
        JobCommonEnvironmentSettings = []
        JobConstraints = None
        JobCustomBehaviors = []
        JobDisplayName = None
        JobId = jobName
        JobManagerTask = None
        JobPreparationTask = None
        JobReleaseTask = None
        JobMetadata = []
        JobPoolInformation = None
        JobPriority = None

        JobTasks = []
        JobSharedLocalFiles = LocalFiles.Zero
    }

    type internal PreparedJobSpecification = {
        PreparedJobCommonEnvironmentSettings : EnvironmentSetting list
        PreparedJobConstraints : JobConstraints option
        PreparedJobCustomBehaviors : BatchClientBehavior list
        PreparedJobDisplayName : string option
        PreparedJobId : JobName
        PreparedJobManagerTask : PreparedTaskSpecification option
        PreparedJobPreparationTask : PreparedTaskSpecification option
        PreparedJobReleaseTask : PreparedTaskSpecification option
        PreparedJobMetadata : MetadataItem list
        PreparedJobPoolInformation : PoolInformation option
        PreparedJobPriority : JobPriority option
    }

    let internal defaultPreparedJobSpecification jobName = {
        PreparedJobCommonEnvironmentSettings = []
        PreparedJobConstraints = None
        PreparedJobCustomBehaviors = []
        PreparedJobDisplayName = None
        PreparedJobId = jobName
        PreparedJobManagerTask = None
        PreparedJobPreparationTask = None
        PreparedJobReleaseTask = None
        PreparedJobMetadata = []
        PreparedJobPoolInformation = None
        PreparedJobPriority = None
    }

    let internal prepareJobForSubmission fileUploader job =
        let ensureJobPreparationTaskWithFiles files = function
        | Some pt -> 
            { pt with TaskLocalFiles = pt.TaskLocalFiles + files }
        | None -> 
            { GetDefaultTaskSpecification (TaskName "default-job-prep") with TaskLocalFiles = files }
        
        { defaultPreparedJobSpecification job.JobId with
            PreparedJobId                        = job.JobId
            PreparedJobConstraints               = job.JobConstraints              
            PreparedJobCommonEnvironmentSettings = job.JobCommonEnvironmentSettings
            PreparedJobCustomBehaviors           = job.JobCustomBehaviors          
            PreparedJobDisplayName               = job.JobDisplayName              
            PreparedJobMetadata                  = job.JobMetadata                 
            PreparedJobPriority                  = job.JobPriority                 
            PreparedJobManagerTask               = job.JobManagerTask     |> Option.map (prepareTaskForSubmission fileUploader)
            PreparedJobReleaseTask               = job.JobReleaseTask     |> Option.map (prepareTaskForSubmission fileUploader)
            PreparedJobPreparationTask           = ensureJobPreparationTaskWithFiles job.JobSharedLocalFiles job.JobPreparationTask 
                                                    |> prepareTaskForSubmission fileUploader 
                                                    |> Some
        }

    let internal toCloudJob (batchClient : BatchClient) preparedJob =
        let cloudJob = batchClient.JobOperations.CreateJob ()

        let (JobName id) = preparedJob.PreparedJobId
        cloudJob.Id                        <- id
        cloudJob.Constraints               <- preparedJob.PreparedJobConstraints |> getOrNull
        cloudJob.CommonEnvironmentSettings <- preparedJob.PreparedJobCommonEnvironmentSettings
        cloudJob.CustomBehaviors           <- preparedJob.PreparedJobCustomBehaviors |> List<BatchClientBehavior>
        cloudJob.DisplayName               <- preparedJob.PreparedJobDisplayName |> getOrNull
        cloudJob.Metadata                  <- preparedJob.PreparedJobMetadata |> List<MetadataItem>
        cloudJob.Priority                  <- preparedJob.PreparedJobPriority |> Option.map (fun (JobPriority j) -> j) |> getOrElse (Nullable(0))
        cloudJob.JobManagerTask            <- preparedJob.PreparedJobManagerTask     |> Option.map (CloudTaskTransforms.toJobManagerTask)     |> getOrNull
        cloudJob.JobReleaseTask            <- preparedJob.PreparedJobReleaseTask     |> Option.map (CloudTaskTransforms.toJobReleaseTask)     |> getOrNull
        cloudJob.JobPreparationTask        <- preparedJob.PreparedJobPreparationTask |> Option.map (CloudTaskTransforms.toJobPreparationTask) |> getOrNull

        cloudJob

    let internal appendToCommandSet t c = {t with TryCatchCommands = t.TryCatchCommands @ [{ TryCatchCommand.Zero with Try = c}]}

    let internal submitJob batchClient poolInformation fileUploader job = 
        let insertCopyCommand hasSharedFiles task = 
            if hasSharedFiles then
                { task with TaskCommandSet = CommonCommands.Windows.CopyJobPrepTaskFilesToJobTask |> appendToCommandSet task.TaskCommandSet  }
            else
                task

        let (JobName jobId) = job.JobId

        let cloudJob = job |> prepareJobForSubmission fileUploader |> toCloudJob batchClient 
        cloudJob.PoolInformation <- poolInformation

        let prepareAndSubmitTask = 
            insertCopyCommand (job.JobSharedLocalFiles = LocalFiles []) 
            >> prepareTaskForSubmission fileUploader 
            >> CloudTaskTransforms.toCloudTask 
            >> submitTask batchClient jobId

        async {
            do! cloudJob.CommitAsync () |> Async.AwaitTask            
            do! job.JobTasks
                |> List.map prepareAndSubmitTask
                |> Threading.Tasks.Task.WhenAll
                |> Async.AwaitTask
        } |> Async.StartAsTask

    let SubmitJobToPoolAsync batchConfiguration storageConfiguration pool job =
        let cloudStorageAccount = GetCloudStorageAccount storageConfiguration
        let stagingContainerName = ContainerName "uploaded-batch-files"
        let fileUploader = UploadFilesToContainer cloudStorageAccount stagingContainerName
        let batchCredentials = GetBatchCredentials batchConfiguration
        async {
            let! batchClient = BatchClient.OpenAsync (batchCredentials) |> Async.AwaitTask
            let poolInformation = EnsurePool batchClient pool
            do! submitJob batchClient poolInformation fileUploader job |> Async.AwaitTask
        } |> Async.StartAsTask

[<AutoOpen>]
module WorkloadOperations =
    let internal getJobForWorkload workload = 
        let getWorkloadUnitTasks =
            let getTaskForWorkloadUnit workloadUnit = 
                let taskName = sprintf "workload-unit-%s" (Guid.NewGuid().ToString("D")) |> TaskName
                { GetDefaultTaskSpecification taskName with
                    TaskCommandSet = workloadUnit.WorkloadUnitCommandSet
                    TaskLocalFiles = workloadUnit.WorkloadUnitLocalFiles
                    TaskRunElevated = workloadUnit.WorkloadUnitRunElevated
                }

            let sweep (m : Map<'a, 'b list>) : Map<'a, 'b> seq =    
                m 
                |> Map.fold (fun ds key values -> 
                        values 
                        |> Seq.collect 
                            (fun i -> seq { yield! ds |> Seq.map (fun d -> d.Add (key, i))}))
                            ([ Map.empty ] |> Seq.ofList)

            let taskTemplates = workload.WorkloadUnitTemplates |> List.map getTaskForWorkloadUnit
            in
            workload.WorkloadArguments 
            |> (fun (WorkloadArguments wa) -> sweep wa)
            |> Seq.collect (fun args -> 
                taskTemplates 
                |> List.map (fun task' -> {task' with TaskArguments = TaskArguments args }))
            |> List.ofSeq

        let jobName = sprintf "workload-%s" (Guid.NewGuid().ToString("D")) |> JobName   
        {GetDefaultJobSpecification jobName with
            JobTasks = getWorkloadUnitTasks
            JobSharedLocalFiles = workload.WorkloadCommonLocalFiles
        }

    let SubmitWorkloadToPoolAsync batchConfiguration storageConfiguration pool workload = 
         getJobForWorkload workload
         |> JobOperations.SubmitJobToPoolAsync batchConfiguration storageConfiguration pool

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
do ()