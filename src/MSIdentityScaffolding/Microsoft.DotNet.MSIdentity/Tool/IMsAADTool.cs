// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;

namespace Microsoft.DotNet.MSIdentity
{
    public interface IMsAADTool 
    {
        Task<ApplicationParameters?> Run();
    }
}