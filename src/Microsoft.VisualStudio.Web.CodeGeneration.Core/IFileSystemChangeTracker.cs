// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface IFileSystemChangeTracker
    {
        IEnumerable<FileSystemChangeInformation> Changes { get; }
        void AddChange(FileSystemChangeInformation info);
    }
}