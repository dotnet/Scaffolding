// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface IFileSystemChangeTracker
    {
        IEnumerable<FileSystemChangeInformation> Changes { get; }
        void AddChange(FileSystemChangeInformation info);
    }
}