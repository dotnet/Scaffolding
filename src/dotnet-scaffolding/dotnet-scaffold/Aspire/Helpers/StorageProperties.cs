// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// StorageProperties defines the structure for storage-related properties in dotnet-scaffold-aspire.
// Used for configuring storage service options and code generation.

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;

internal class StorageProperties
{
    public required string AddMethodName { get; init; }
    public required string VariableName { get; init; }
    public required string AddClientMethodName { get; init; }
}
