// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public const string AddDirectory = "add_directory";

        /// <summary>
        /// A directory existing on disk is removed.
        /// </summary>
        public const string RemoveDirectory = "remove_directory";
    }
}
