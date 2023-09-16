// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform
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
