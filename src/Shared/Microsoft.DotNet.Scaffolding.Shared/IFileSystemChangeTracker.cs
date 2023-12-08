// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared
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
