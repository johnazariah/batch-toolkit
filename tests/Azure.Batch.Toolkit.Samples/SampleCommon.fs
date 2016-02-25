namespace Azure.Batch.Toolkit.Samples

[<AutoOpen>]
module SampleCommon =
    open Batch.Toolkit
    open Batch.Toolkit.DSL
    open Batch.Toolkit.PoolOperations
    open Batch.Toolkit.WorkloadOperations

    let runSampleWorkload workload = 
        let pool = NamedPool { NamedPoolName = PoolName "sample-pool"; NamedPoolSpecification = GetDefaultPoolSpecification }        
        let (batchConfig, storageConfig) = 
            succeed {        
                let! batchConfig = readConfig<BatchConfiguration>("batch-config.json")
                let! storageConfig = readConfig<StorageConfiguration>("storage-config.json")
                return (batchConfig, storageConfig)
            } |> getOrThrow

        workload |> RunWorkloadOnPool batchConfig storageConfig pool