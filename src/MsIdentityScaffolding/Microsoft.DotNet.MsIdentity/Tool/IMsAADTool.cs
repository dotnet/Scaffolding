// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.DotNet.MsIdentity.AuthenticationParameters;

namespace Microsoft.DotNet.MsIdentity
{
    public interface IMsAADTool 
    {
        Task<ApplicationParameters?> Run();
    }
}