// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.DotNet.Scaffolding.Shared
{
    /// <summary>
    /// Indicates the type of file system change
    /// </summary>
    public class FileSystemChangeType
    {
        /// <summary>
        /// A new file is added.
        /// </summary>
        public const string AddFile = "add_file";

        /// <summary>
        /// A file existing on disk was edited.
        /// </summary>
        public const string EditFile = "edit_file";

        /// <summary>
        /// A file existing on disk is deleted.
        /// </summary>
        public const string DeleteFile = "delete_file";

        /// <summary>
        /// A new directory is added.
        /// </summary>
        public const string AddDirectory ="add_directory";

        /// <summary>
        /// A directory existing on disk is removed.
        /// </summary>
        public const string RemoveDirectory = "remove_directory";
    }
}
