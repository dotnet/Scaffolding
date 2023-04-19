// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    public interface IMsAADTool
    {
        Task<ApplicationParameters?> Run();
    }
}
