// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General
{
    internal class IdentityDbContextModel
    {
        internal string DbContextNamespace { get; set; }
        internal string DbContextName { get; set; }
        internal IdentityApplicationUserModel UserClassModel { get; set; }
    }
}
