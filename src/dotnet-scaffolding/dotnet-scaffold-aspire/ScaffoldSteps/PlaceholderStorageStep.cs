// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

internal class PlaceholderStorageStep : PlaceholderStepBase<StorageCommand>
{
    public PlaceholderStorageStep(IFileSystem fileSystem, ILogger<PlaceholderStorageStep> logger)
        : base(new StorageCommand(fileSystem, logger))
    {

    }
}
