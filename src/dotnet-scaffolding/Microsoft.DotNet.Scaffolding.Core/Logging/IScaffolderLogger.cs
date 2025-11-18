// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

public interface IScaffolderLogger : ILogger
{
    public void LogError(string message);

    public void LogInformation(string message);
}
