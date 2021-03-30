// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DotNet.MsIdentity.MicrosoftIdentityPlatformApplication
{
    internal class ResourceAndScope
    {
        public ResourceAndScope(string resource, string scope)
        {
            Resource = resource;
            Scope = scope;
        }

        public string Resource { get; set; }
        public string Scope { get; set; }
        public string? ResourceServicePrincipalId { get; set; }
    }
}
