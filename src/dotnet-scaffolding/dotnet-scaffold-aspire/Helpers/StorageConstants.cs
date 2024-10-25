// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;

internal static class StorageConstants
{
    public const string TablesVariableName = "entries";
    public const string BlobsVariableName = "blobs";
    public const string QueuesVariableName = "queues";
    public const string AddQueuesMethodName = "AddQueues";
    public const string AddBlobsMethodName = "AddBlobs";
    public const string AddTablesMethodName = "AddTables";
    public const string BlobsClientMethodName = "AddAzureBlobClient";
    public const string TablesClientMethodName = "AddAzureTableClient";
    public const string QueuesClientMethodName = "AddAzureQueueClient";
    public static StorageProperties TableProperties = new()
    {
        VariableName = TablesVariableName,
        AddMethodName = AddTablesMethodName,
        AddClientMethodName = TablesClientMethodName
    };

    public static StorageProperties QueueProperties = new()
    {
        VariableName = QueuesVariableName,
        AddMethodName = AddQueuesMethodName,
        AddClientMethodName = QueuesClientMethodName
    };

    public static StorageProperties BlobProperties = new()
    {
        VariableName = BlobsVariableName,
        AddMethodName = AddBlobsMethodName,
        AddClientMethodName = BlobsClientMethodName
    };

    public static Dictionary<string, StorageProperties> StoragePropertiesDict = new()
    {
        { "azure-storage-queues", QueueProperties },
        { "azure-storage-blobs", BlobProperties },
        { "azure-data-tables", TableProperties }
    };
}
