namespace Batch.Toolkit

open Microsoft.Azure.Batch

module JobOperations =
    let SubmitJobToPoolAsync batchConfiguration storageConfiguration pool job =
        let cloudStorageAccount = GetCloudStorageAccount storageConfiguration
        let stagingContainerName = storageConfiguration.StagingContainerName |> ContainerName
        let fileUploader = UploadFilesToContainer cloudStorageAccount stagingContainerName
        let batchCredentials = GetBatchCredentials batchConfiguration
        async {
            let! batchClient = BatchClient.OpenAsync (batchCredentials) |> Async.AwaitTask
            batchClient.CustomBehaviors.Add(RetryPolicyProvider.LinearRetryProvider(System.TimeSpan.FromSeconds(10.0), 3));

            let poolInformation = EnsurePool batchClient pool

            do! submitJob batchClient poolInformation fileUploader job |> Async.AwaitTask
        } |> Async.StartAsTask

module WorkloadOperations =
    let SubmitWorkloadToPoolAsync batchConfiguration storageConfiguration pool workload = 
        let workloadName = 
            System.DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") 
            |> int64 
            |> sprintf "workload-%d"
        
        workload
        |> getJobForWorkload workloadName
        |> JobOperations.SubmitJobToPoolAsync batchConfiguration storageConfiguration pool

    let RunWorkloadOnPool batchConfiguration storageConfiguration pool workload =
        workload 
        |> SubmitWorkloadToPoolAsync batchConfiguration storageConfiguration pool 
        |> (Async.AwaitTask >> Async.RunSynchronously)

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Batch.Toolkit.Tests")>]
do ()