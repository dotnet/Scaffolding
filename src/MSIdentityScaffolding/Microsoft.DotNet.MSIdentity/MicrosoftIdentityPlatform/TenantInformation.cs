// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform
{
    internal class TenantInfo
    {
        public string? TenantId { get; set; }
        public string? DisplayName { get; set; }
        public string? DefaultDomain { get; set; }
        public string? TenantType { get; set; }
    }
}
