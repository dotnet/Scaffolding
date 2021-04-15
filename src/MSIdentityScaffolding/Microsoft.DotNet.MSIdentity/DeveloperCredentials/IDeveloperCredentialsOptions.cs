// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DotNet.MSIdentity.DeveloperCredentials
{
    interface IDeveloperCredentialsOptions
    {
        /// <summary>
        /// Identity (for instance joe@cotoso.com) that is allowed to
        /// provision the application in the tenant. Optional if you want
        /// to use the developer credentials (Visual Studio).
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Tenant ID of the application (optional if the user belongs to
        /// only one tenant Id).
        /// </summary>
        public string? TenantId { get; set; }
    }
}
