// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class FileSystemChangeTracker : IFileSystemChangeTracker
    {
        private object _syncobject = new object();

        private Dictionary<string, FileSystemChangeInformation> _changes = new Dictionary<string, FileSystemChangeInformation>();
        public IEnumerable<FileSystemChangeInformation> Changes
        {
            get
            {
                lock (_syncobject)
                {
                    return _changes.Values;
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
    }
}