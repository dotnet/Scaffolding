// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange
{
    public class FileSystemChangeInformation
    {
        public string FullPath { get; set; }
        public string FileSystemChangeType { get; set; }
        public string FileContents { get; set; }
    }
}
