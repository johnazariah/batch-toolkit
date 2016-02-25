namespace Azure.Batch.Toolkit.Samples

///<summary>
/// This sample serves as an example of how to use the DSL to specify a very simple workload.
///
/// The workload consists of a single workload unit template, which in turn consists of just a single parametrized command in its commandset.
/// The workload specifies a range of values for the parameter in the parametrized command.
///
/// This workload specification will be converted into a single Azure Batch CloudJob, associated with a single CloudTask for each instance of the template-parameter pair.
///
/// The sample will run against the credentials and pool specified in the SampleCommon module.
///</summary>
module Sample1 =
    open Batch.Toolkit.DSL

    let helloUserCommand = 
        ``:parametric_cmd`` "echo 'Hello %user%'" ``:with_params`` ["user"]
    
    let workloadUnitTemplate = 
        ``:unit_template`` 
            ``:admin`` false 
            ``:main`` 
                [
                    ``:do`` helloUserCommand
                ] 
            ``:finally``
                []
            ``:files`` 
                []

    let workload =
        ``:workload``
            ``:unit_templates`` 
                [
                    workloadUnitTemplate 
                ]
            ``:files`` 
                []
            ``:arguments`` 
                [
                    ``:range`` "user" ``:over`` ["John"; "Ivan"; "Pablo"]
                ]

    [<EntryPoint>]
    workload |> runSampleWorkload