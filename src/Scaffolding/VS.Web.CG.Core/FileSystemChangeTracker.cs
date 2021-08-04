// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class FileSystemChangeTracker : IFileSystemChangeTracker
    {
        private static readonly StringComparer PathComparisonType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        private ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

        private Dictionary<string, FileSystemChangeInformation> _changes = new Dictionary<string, FileSystemChangeInformation>(PathComparisonType);
        public IEnumerable<FileSystemChangeInformation> Changes
        {
            get
            {
                readerWriterLock.EnterReadLock();
                var returnvalue = _changes.Values.ToList();
                readerWriterLock.ExitReadLock();
                return returnvalue;
            }
        }

        public void AddChange(FileSystemChangeInformation fileSystemChangeInfo)
        {
            if (fileSystemChangeInfo == null)
            {
                throw new ArgumentNullException(nameof(fileSystemChangeInfo));
            }

            // The last change always wins.
            readerWriterLock.EnterWriteLock();
            _changes[fileSystemChangeInfo.FullPath] = fileSystemChangeInfo;
            readerWriterLock.ExitWriteLock();
        }

        public void RemoveChange(FileSystemChangeInformation fileSystemChangeInfo)
        {
            if (fileSystemChangeInfo == null)
            {
                throw new ArgumentNullException(nameof(fileSystemChangeInfo));
            }

            readerWriterLock.EnterWriteLock();
            if (_changes.ContainsKey(fileSystemChangeInfo.FullPath))
            {
                _changes.Remove(fileSystemChangeInfo.FullPath);
            }
            readerWriterLock.ExitWriteLock();
        }

        public void RemoveChanges(IEnumerable<FileSystemChangeInformation> fileSystemChanges)
        {
            if (fileSystemChanges == null)
            {
                throw new ArgumentNullException(nameof(fileSystemChanges));
            }

            readerWriterLock.EnterWriteLock();
            foreach (var changeInfo in fileSystemChanges)
            {
                if (_changes.ContainsKey(changeInfo.FullPath))
                {
                    _changes.Remove(changeInfo.FullPath);
                }
            }
            readerWriterLock.ExitWriteLock();
        }

        public void ClearChanges()
        {
            readerWriterLock.EnterWriteLock();
            _changes.Clear();
            readerWriterLock.ExitWriteLock();
        }

    }
}
