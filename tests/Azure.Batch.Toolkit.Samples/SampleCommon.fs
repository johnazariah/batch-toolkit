namespace Azure.Batch.Toolkit.Samples

///<summary>
/// This sample serves as an example of how to use the Batch Toolkit functions.
///
/// We need to construct instances of BatchConfiguration and StorageConfiguration, filling them with appropriate credentials and configuration.
///
/// Given an instance of WorkloadSpecification, this function will run it on a default pool called "sample-pool"
///</summary>
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