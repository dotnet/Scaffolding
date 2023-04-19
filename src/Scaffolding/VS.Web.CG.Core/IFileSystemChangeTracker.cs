// Copyright (c) .NET Foundation. All rights reserved.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface IFileSystemChangeTracker
    {
        IEnumerable<FileSystemChangeInformation> Changes { get; }
        void AddChange(FileSystemChangeInformation info);
        void RemoveChange(FileSystemChangeInformation info);
        void ClearChanges();
        void RemoveChanges(IEnumerable<FileSystemChangeInformation> subDirectoryChanges);
    }
}
