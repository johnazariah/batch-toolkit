(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Azure.Batch.Toolkit"

(**
Tutorial 2: The Toolkit Job Object Model
========================

The purpose of this tutorial is to introduce the various components in the Toolkit's Job Object Model.

When you are done, you should have:

1. An understanding of the object model of jobs and workloads
1. An understanding of how to compose workloads incrementally

Remember, you should have run the [Getting Set Up](tutorial0.html) tutorial first!
*)

(**
#### Reference and Namespace

The first thing to do is to reference the toolkit. _Note that it is packaged in **Batch.Toolkit.dll**_

The next thing is to open the namespace. _Note that this is **Batch.Toolkit**_

The DSL is found in the 
*)
#r "Batch.Toolkit.dll"
open Batch.Toolkit
open Batch.Toolkit.DSL

(**
#### Object Model: Command

A ``Command`` is the basic unit of execution. This is typically the name of your executable file or batch script.

As we've seen before, a ``Command`` can come in one of two flavours:

* A ``SimpleCommand`` is just a string with the command line of the executable you want to run. You can have spaces and static arguments passed to the executable name as part of a simple command
* A ``ParametrizedCommand`` pairs a command line _template_ with a collection of parameter names, which can be replaced by the toolkit with a range of values. Surround the parameter name with ``%`` in the command line to identify it as a placeholder.

You can create instances of these as follows:
*)

let simpleCommand = SimpleCommand "echo 'Hello, World!'"
let parametrizedCommand = { Command = "echo 'Hello, %user%"; Parameters = ["user"] }

(**
#### Object Model: CommandWithErrorHandler

A ``CommandWithErrorHandler`` is a construct which allows you to specify a ``Command`` to execute, and optionally a ``Command`` to run if it fails. You can use this to represent an error-recoverable operation; or an operation paired with its compensation.

You can create an instance of a ``CommandWithErrorHandler`` as follows:
*)

let recoverableCommand = { Try = parametrizedCommand; OnError = Some simpleCommand }

(**
#### Object Model: CommandSet

The toolkit provides a way to collect groups of commands so we can build up a more complex workload.

A ``CommandSet`` has a ``MainCommands`` block, which is a list of ``CommandWithErrorHandler`` objects; and a ``FinallyCommands`` block, which is a list of ``Command``s

You can create an instance of a ``CommandWithErrorHandler`` as follows:
*)

let adiosCommand = SimpleCommand "echo 'Goodbye, Cruel World!'"
let sayHelloAndGoodbye = { MainCommands = [recoverableCommand]; FinallyCommands = [adiosCommand]}

(**

You can use the ``CommandSet`` object to model a block of work which needs to be done by a task, followed by a block of work to finalize the work. 

The ``FinallyCommands`` block is useful to specify commands to copy up the results of the work done into Azure storage, for example.

#### Note : Compositionality

The ``CommandSet`` type is augmented with functions to make it a structure called a "Monoid". This means that a set of ``CommandSet`` objects can be combined into a single ``CommandSet``. 

The implication of this is that ``CommandSet`` objects can be reused compositionally - new ``CommandSet`` objects can be built up from many, pre-developed ``CommandSet``s.
*)

let copyResultsToAzureCommand = SimpleCommand "run-this-command-to-copy-results-to-azure"
let copyResultsToAzure = 
    {
         MainCommands = []
         FinallyCommands = [copyResultsToAzureCommand]
    }

let combinedCommandSet = sayHelloAndGoodbye + copyResultsToAzure

(**
``combinedCommandSet`` will now have the following structure:

    { 
        MainCommands = 
            [
                recoverableCommand
            ] 
        FinallyCommands = 
            [
                adiosCommand
                copyResultsToAzureCommand
            ]
    }


#### Note : Script Generation

You only need to supply the construct itself. The toolkit will automatically generate a script to:

* For each ``CommandWithErrorHandler`` in the ``MainCommands`` block
    * Execute the ``Try`` part of the construct
    * Check if the error status represents a failure
    * Execute the ``OnError`` part of the construct only if an error was signalled
* For each ``Command`` in the ``FinallyCommands`` block
    * Execute the command
    
The toolkit will craft an instance of an Azure Batch ``CloudTask``, attach the script file its resource, and set its CommandLine to run the script.

#### Object Model: LocalFiles & UploadedFiles

A Batch task consists of commands and resources. A resource is a file associated with the task.

Since the task executes on a remote machine, there are two types of files:

* ``LocalFiles`` are files that are located locally. You can refer to them using ``System.IO.FileInfo`` instances.
* ``UploadedFiles`` are files that have already been uploaded and are located in Azure. You can refer to them using ``Microsoft.Azure.Batch.FileStaging.ResourceFile`` instances.

Like ``CommandSet``s, ``LocalFiles`` and ``UploadedFiles`` are also monoids so you can build them up incrementally.

*)

let localFiles = LocalFiles [ System.IO.FileInfo @"resources\7zip.msi" ]
let emptyRemoteFiles = UploadedFiles []

(**
The ``LocalFiles`` and ``UploadedFiles`` types are also monoidally foldable.

#### Note : File upload

The toolkit automatically includes a file upload phase, where the files associated with a ``WorkloadUnitTemplate`` (and ultimately with a ``CloudTask`` object), are uploaded into a container specified by the ``StagingContainerName`` member of the ``StorageConfiguration`` object.

_[Tutorial 1](tutorial1.html) has an example of how the ``StorageConfiguration`` object is set up and used._

#### Object Model: WorkloadUnitTemplate

We can collect together a ``CommandSet`` and a ``LocalFiles`` collection into something that forms the recipe for a single unit of computation to be executed in Batch.

This object is named ``WorkloadUnitTemplate``, bearing in mind that the ``Command``s in the ``CommandSet`` are potentially parametrized. 

The toolkit expresses the ``WorkloadUnitTemplate`` into a separate``CloudTask`` object for each unique set of parameter values.

*)

let simpleWorkloadUnitTemplate = 
    {
        WorkloadUnitRunElevated = true
        WorkloadUnitCommandSet = combinedCommandSet
        WorkloadUnitLocalFiles = localFiles
    }

(**

#### Object Model: WorkloadArguments

As we have seen, the ``CommandSet`` member of a ``WorkloadUnitTemplate`` has is composed of multiple ``Command``s. Any or all of these ``Command``s can be ``ParametrizedCommand``s, and each ``ParametrizedCommand`` may have multiple parameters.

We need a way to define the range of values to be assigned, in turn, to each parameter.

The ``WorkloadArguments`` object allows us to define these collections and thereby specify the "parametric sweep" of the workload.

The ``WorkloadArguments`` object is effectively a dictionary mapping the key (the parameter name - a string) to a list of values (the range of parameter values). 

You can consttruct one as follows: 
*)

let workloadNames = 
    [ 
        ("name", ["John"; "Ivan"; "Mark"])
    ] 
    |> Map.ofSeq 
    |> WorkloadArguments

(**

The ``WorkloadArguments`` object is *also* a monoid! 

You can combine entire groups of argument lists incrementally, and it will merge in parameter names and values.

#### Object Model: WorkloadSpecification

We are now _finally_ able to define what our complete workload is going to look like:

* A template for the computation and computation-specific resources
* A set of parameter ranges

In fact we can further gneralize and support _multiple_ ``WorkloadUnitTemplate``s, and introduce the concept of files shared across all ``WorkloadUnitTemplate``s as well.

The toolkit can express such an object as a ``CloudJob``:

*)

let workload = 
    {
        WorkloadUnitTemplates = [simpleWorkloadUnitTemplate]
        WorkloadCommonLocalFiles = []
        WorkloadArguments = workloadNames
    }

(**

For good measure, the ``WorkloadSpecification`` is also a monoid! 

We can incrementally build up simple workloads, and smash them together to make a complex workload, facilitating compositional re-use.

We have seen how we can incrementally build up a workload from smaller bits (each of which may reuse previously defined pieces), and define a batch ``CloudJob``.

Other tutorials will cover the Pool Object Model, and describe how you can execute the workload itself against a pool of Azure Virtual Machines.
*)
