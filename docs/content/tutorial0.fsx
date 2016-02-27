(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Azure.Batch.Toolkit"

(**
Tutorial 0: Getting Set Up
========================

The purpose of this tutorial is to set up your development ecosystem so you can use the Toolkit to interact with Azure Batch.

When you are done, you should have:
1. Some commonly used tools you will need to interact with Azure Batch
1. A VS2013/2015 workspace where you can write programs against Azure Batch
1. An experience of having interacted with the Batch Service
*)

(**
#### Get yourself an Azure Account and Azure Batch Account

You're going to need an active Azure Account to be able to work with batch services.

If you don't have one already, sign up for a free trial [here][AZURE_SIGNUP].

Additionally, you're going to need an Azure _Batch_ Account, which will be associated with your Azure Account for billing purposes.
Your Azure Batch Account will be your ticket to using Azure Batch Services, as all interactions with the service either implicitly or explicitly require you to provide Azure Batch Credentials.

If you don't have one already, get one by following the instructions [here][AZURE_BATCH_SIGNUP].

[AZURE_SIGNUP]: https://azure.microsoft.com/en-us/pricing/free-trial/?WT.srch=1&WT.mc_ID=SEM_b372mhEg&bknode=BlueKai
[AZURE_BATCH_SIGNUP]: https://azure.microsoft.com/en-us/documentation/articles/batch-account-create-portal/
*)

(**
#### Get a good storage viewer

There are several viewers of Azure storage that are out there.

1. Commercial solutions like [CloudBerry](http://www.cloudberrylab.com/free-microsoft-azure-explorer.aspx) and [Cerebrata](http://www.cerebrata.com/products/azure-management-studio/introduction) have offerings
1. Visual Studio 2013/2015 has a decent one built in
1. Microsoft [Azure Storage Explorer](http://storageexplorer.com/) is free

Use whatever you like. You'll need to be comfortable interacting with blob storage to see files uploaded and downloaded
*)

(** 
#### Build Batch Explorer

This is a very useful tool to have when interacting with Batch Services. The Batch Services team have released the source for this tool as an (excellent) example of how to interact with the service, but it's a very useful tool in its own right.

You're going to have to get the code from [github](https://github.com/Azure/azure-batch-samples) and build the BatchExplorer project found at `CSharp\BatchExplorer`. Keep the samples project around so you'll get a sense of what can be achieved going down to the SDK yourself!
*)


(**
#### Install the toolkit from NuGet

If you haven't already, install the Azure Batch Toolkit from NuGet (or introduce it into your paket.dependencies file if you're using Paket)

    PM> Install-Package Azure.Batch.Toolkit
*)

(**
You're now good to go! Try one of the other tutorials listed on the side of the page.
*)