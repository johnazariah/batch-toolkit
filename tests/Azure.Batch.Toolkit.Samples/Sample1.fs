namespace Azure.Batch.Toolkit.Samples

module Sample1 =
    open Batch.Toolkit
    open Batch.Toolkit.DSL

    let helloUserCommand = 
        ``:parametric_cmd`` "echo 'Hello %user%'" ``:with_params`` ["user"]
    
    let workloadUnitTemplate = 
        ``:unit`` 
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
            ``:units`` 
                [
                    workloadUnitTemplate 
                ]
            ``:files`` 
                []
            ``:arguments`` 
                [
                    ``:range`` "user" ``:over`` ["John"; "Ivan"; "Pablo"]
                ]

    workload |> runSampleWorkload