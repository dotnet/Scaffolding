// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange
{
    public class FileSystemChangeType
    {
        public const string AddFile = "add_file";
        public const string EditFile = "edit_file";
        public const string DeleteFile = "delete_file";
        public const string AddDirectory ="add_directory";
        public const string RemoveDirectory = "remove_directory";
    }
}
