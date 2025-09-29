// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// DatabaseProperties defines properties specific to database scenarios in dotnet-scaffold-aspire.
// Used for configuring database-related options and code generation.

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;

/// <summary>
/// Properties specific to dotnet-scaffold-aspire database scenario
/// </summary>
internal class DatabaseProperties
{
    public required string AspireAddDbMethod { get; set; }
    public required string AspireAddDbContextMethod { get; set; }
    public required string AspireDbName { get; set; }
    public required string AspireDbType { get; set; }
}
