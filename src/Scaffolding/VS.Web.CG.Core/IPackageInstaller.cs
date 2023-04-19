// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface IPackageInstaller
    {
        Task InstallPackages(IEnumerable<PackageMetadata> packages);
    }
}
