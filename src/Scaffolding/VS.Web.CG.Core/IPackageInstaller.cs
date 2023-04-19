// Copyright (c) .NET Foundation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface IPackageInstaller
    {
        Task InstallPackages(IEnumerable<PackageMetadata> packages);
    }
}
