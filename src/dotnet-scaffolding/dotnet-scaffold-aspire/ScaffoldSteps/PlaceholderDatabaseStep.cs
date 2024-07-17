// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

internal class PlaceholderDatabaseStep : PlaceholderStepBase<DatabaseCommand>
{
    public PlaceholderDatabaseStep(IFileSystem fileSystem, ILogger logger)
        :base(new DatabaseCommand(fileSystem, logger))
    {
        
    }
}
