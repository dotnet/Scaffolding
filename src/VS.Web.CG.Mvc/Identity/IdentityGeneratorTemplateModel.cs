// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{

    public class IdentityGeneratorTemplateModel
    {
        public string Namespace { get; set; }
        public string DbContextNamespace { get; set; }
        public string ApplicationName { get; set; }
    }
}