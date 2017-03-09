// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange
{
    /// <summary>
    /// Represents information about a file system change.
    /// </summary>
    public class FileSystemChangeInformation
    {
        /// <summary>
        /// Full path of the changed file/ directory.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// See <see cref="FileSystemChange.FileSystemChangeType"/> for possible values.
        /// </summary>
        public string FileSystemChangeType { get; set; }

        /// <summary>
        /// Contents of the file if <see cref="FileSystemChangeType"/>
        /// is <see cref="FileSystemChange.FileSystemChangeType.AddFile"/>
        /// or <see cref="FileSystemChange.FileSystemChangeType.EditFile"/>
        /// </summary>
        public string FileContents { get; set; }
    }
}
