// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public interface IApplicationInfo
    {
        string ApplicationBasePath { get; }
        string ApplicationName { get; }
        string ApplicationConfiguration { get; }
    }
}
