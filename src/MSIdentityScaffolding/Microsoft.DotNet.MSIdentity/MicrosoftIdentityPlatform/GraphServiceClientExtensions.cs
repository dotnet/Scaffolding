// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Graph.Models;

namespace Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatform
{
    public static class GraphServiceClientExtensions
    {
        public static string? GetTenantType(this Organization tenant)
        {
            return tenant.AdditionalData["tenantType"]?.ToString();
        }
    }
}
