// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.ProjectModel;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public interface ILibraryManager
    {
        IEnumerable<LibraryDescription> GetLibraries();
        LibraryDescription GetLibrary(string name);
        IEnumerable<LibraryDescription> GetReferencingLibraries(string name);
    }
}
