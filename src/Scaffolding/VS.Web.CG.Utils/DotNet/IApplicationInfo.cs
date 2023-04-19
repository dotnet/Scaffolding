// Copyright (c) .NET Foundation. All rights reserved.
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
