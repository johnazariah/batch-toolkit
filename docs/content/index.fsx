(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Azure.Batch.Toolkit"

(**
Introduction to Azure Batch Toolkit
========================

The [Azure Batch Service][BATCH_SVC] is an exciting offering from Microsoft allowing you to build systems that can run workloads in the cloud. 

An easy way to get started with Azure Batch is to identify command-line programs that are currently run on premise and "lift-and-shift" those programs to run on virtual machines in Azure.

There's a [ton of documentation][BATCH_DOCS] of how to do this because the Batch Service provides a rich selection of options:

* You could use the [Powershell][PS] interface to script programs to do this.
* You could write programs using a variety of languages (C#, Python) using the [SDK and Client Libraries][SDK] available. 
* You could write programs using a language that isn't currently supported by interacting with the [REST interface][REST]. 

These approaches afford the greatest flexibility possible in interacting with the Batch Service, but they require the developers to acquire a working knowledge of the ecosystem and to become familiar with common usage patterns and idioms. 
There are [several samples][SAMPLES] to guide this effort.

However, in many cases, the developer is more familiar with their own domain, and not necessarily able (or willing) to expend the effort to become an expert in programming Azure Batch. 
This toolkit, developed in F#, affords a simpler introduction to using the Batch Service by abstracting out the interactions with the Batch Service and allowing the developer to focus on defining and crafting their own workflows.

The best way to use this toolkit would be to write your workflow in F#, because that allows for the use of the little DSL present in the toolkit.
The library is, however, interoperable with C#, and it's possible (although not as elegant) to develop these workflows there as well.

An important thing to note is that the workload you are designing can involve executables written in any language - and indeed, which run on any supported platform. 
_The F# (or C#) requirement to use the toolkit is limited __only__ to the workflow definition and submission stages of your development experience_

Let's get started by installing the library from NuGet

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The Azure.Batch.Toolkit library can be <a href="https://nuget.org/packages/Azure.Batch.Toolkit">installed from NuGet</a>:
      <pre>PM> Install-Package Azure.Batch.Toolkit</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

[BATCH_SVC]: https://azure.microsoft.com/en-us/services/batch/
[BATCH_DOCS]: https://azure.microsoft.com/en-us/documentation/services/batch/
[PS]: https://msdn.microsoft.com/library/azure/dn865466.aspx
[SDK]: https://msdn.microsoft.com/library/azure/dn865466.aspx
[REST]: https://msdn.microsoft.com/library/azure/dn820158.aspx
[SAMPLES]: https://github.com/Azure/azure-batch-samples/tree/master/CSharp
*)

(**
Samples & documentation
-----------------------

1. [Tutorial 0][T0] Get your development environment set up for using Azure Batch, and the toolkit
1. [Tutorial 1][T1] Use the DSL to create your first workload with a simple command and submit it

[T0]: tutorial0.html
[T1]: tutorial1.html
*)

(**
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/johnazariah/batch-toolkit/tree/master/docs/content
  [gh]: https://github.com/johnazariah/batch-toolkit
  [issues]: https://github.com/johnazariah/batch-toolkit/issues
  [readme]: https://github.com/johnazariah/batch-toolkit/blob/master/README.md
  [license]: https://github.com/johnazariah/batch-toolkit/blob/master/LICENSE.txt
*)
