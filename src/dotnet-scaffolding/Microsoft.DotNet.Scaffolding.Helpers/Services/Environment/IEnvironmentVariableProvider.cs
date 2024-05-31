// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;

internal interface IEnvironmentVariableProvider
{
    /// <summary>
    /// Returns environment variables to be set in current process.
    /// </summary>
    /// <returns></returns>
    ValueTask<IEnumerable<KeyValuePair<string, string>>?> GetEnvironmentVariablesAsync();
}
