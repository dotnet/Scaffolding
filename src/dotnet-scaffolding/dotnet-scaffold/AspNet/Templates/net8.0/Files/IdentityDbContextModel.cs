// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net8.Files
{
    internal class IdentityDbContextModel
    {
        internal required string DbContextNamespace { get; set; }
        internal required string DbContextName { get; set; }
        internal required IdentityApplicationUserModel UserClassModel { get; set; }
    }
}
