namespace Batch.Toolkit
open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.Azure.Batch
open Microsoft.Azure.Batch.FileStaging

module DSL =
    let ``:cmd`` = SimpleCommand

    let ``:with_params`` = None
    let ``:parametric_cmd`` command _ parameters = ParametrizedCommand { Command = command; Parameters = parameters }

    let ``:do`` c = {Try = c; OnError = None }
    
    let ``:with`` = None
    let ``:try`` c _ h = { Try = c; OnError = Some h }

    let ``:main`` = None
    let ``:admin`` = None
    let ``:finally`` = None
    let ``:files`` = None
    let ``:unit_template`` 
        _ runAsAdmin 
        _ mainBlock
        _ finallyBlock
        _ localFiles = 
        {
            WorkloadUnitCommandSet = { MainCommands = mainBlock; FinallyCommands = finallyBlock } 
            WorkloadUnitLocalFiles = localFiles |> LocalFiles
            WorkloadUnitRunElevated = runAsAdmin
        }
    
    let ``:over`` = None
    let ``:range`` parameterName _ parameterValues = 
        (parameterName, parameterValues |> Set.ofList)

    let ``:unit_templates`` = None
    let ``:arguments`` = None
    
    let ``:workload`` 
            _ workloadUnitTemplates 
            _ commonFiles 
            _ arguments =
        {
            WorkloadUnitTemplates = workloadUnitTemplates
            WorkloadCommonLocalFiles = commonFiles |> LocalFiles
            WorkloadArguments = arguments |> Map.ofSeq |> WorkloadArguments
        }

    let ``:exit`` = SimpleCommand "goto :EXIT"

module internal Sample =
    open DSL

    let CopyOutputToAzure = ``:cmd`` "echo 'Copying output to Azure'"
    let SayHelloWorld = ``:cmd`` "echo 'Hello, world!'"
    let SayGoodbye = ``:cmd`` "echo 'Goodbye'"
    let ReadItemFromQueue = ``:parametric_cmd`` "echo 'Reading from queue %queue_name%'" ``:with_params`` ["queue_name"]
    
    let workload =
        ``:workload``
            ``:unit_templates`` 
                [
                    (``:unit_template`` 
                        ``:admin`` true 
                        ``:main`` 
                            [
                                ``:do`` SayHelloWorld
                                ``:try`` 
                                    (``:parametric_cmd`` "echo 'Hello %user%'" ``:with_params`` ["user"]) 
                                    ``:with`` ``:exit``
                            ] 
                        ``:finally``
                            [
                                CopyOutputToAzure
                                SayGoodbye
                            ]
                        ``:files`` []) 
                ]
            ``:files`` 
                []
            ``:arguments`` 
                [
                    ``:range`` "%user%" ``:over`` ["John"; "Ivan"; "Pablo"]
                ]