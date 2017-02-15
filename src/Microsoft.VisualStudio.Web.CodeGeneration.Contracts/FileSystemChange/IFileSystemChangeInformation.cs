// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.FileSystemChange
{
    interface IFileSystemChangeInformation
    {
        string FullPath { get; }
        string FileSystemChangeType { get; }
        string FileContents { get; }
    }
}
