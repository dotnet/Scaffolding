// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

internal class PlaceholderDatabaseStep : PlaceholderStepBase<DatabaseCommand>
{
    public PlaceholderDatabaseStep(IFileSystem fileSystem, ILogger<PlaceholderDatabaseStep> logger)
        :base(new DatabaseCommand(fileSystem, logger))
    {
        
    }
}
