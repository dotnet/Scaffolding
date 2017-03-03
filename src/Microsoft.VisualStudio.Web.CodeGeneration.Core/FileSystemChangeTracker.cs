// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class FileSystemChangeTracker : IFileSystemChangeTracker
    {
        private object _syncobject = new object();

        private Dictionary<string, FileSystemChangeInformation> _changes = new Dictionary<string, FileSystemChangeInformation>(StringComparer.OrdinalIgnoreCase);
        public IEnumerable<FileSystemChangeInformation> Changes
        {
            get
            {
                lock (_syncobject)
                {
                    return _changes.Values.ToList();
                }
            }
        }

        public void AddChange(FileSystemChangeInformation fileSystemChangeInfo)
        {
            if (fileSystemChangeInfo == null)
            {
                throw new ArgumentNullException(nameof(fileSystemChangeInfo));
            }

            // The last change always wins.
            lock (_syncobject)
            {
                _changes[fileSystemChangeInfo.FullPath] = fileSystemChangeInfo;
            }
        }

        public void RemoveChange(FileSystemChangeInformation fileSystemChangeInfo)
        {
            if (fileSystemChangeInfo == null)
            {
                throw new ArgumentNullException(nameof(fileSystemChangeInfo));
            }

            lock (_syncobject)
            {
                if (_changes.ContainsKey(fileSystemChangeInfo.FullPath))
                {
                    _changes.Remove(fileSystemChangeInfo.FullPath);
                }
            }
        }

        public void RemoveChanges(IEnumerable<FileSystemChangeInformation> fileSystemChanges)
        {
            if (fileSystemChanges == null)
            {
                throw new ArgumentNullException(nameof(fileSystemChanges));
            }

            lock (_syncobject)
            {
                foreach (var changeInfo in fileSystemChanges)
                {
                    if (_changes.ContainsKey(changeInfo.FullPath))
                    {
                        _changes.Remove(changeInfo.FullPath);
                    }
                }
            }
        }

        public void ClearChanges()
        {
            lock (_syncobject)
            {
                _changes.Clear();
            }
        }

    }
}