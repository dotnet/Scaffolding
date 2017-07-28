// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public interface IFilesLocator
    {
        /// <summary>
        /// Returns the first file found within the given search paths.
        /// </summary>
        /// <remarks>
        /// A recursive search is done within each search path 
        /// and the file matching file with the fileName is returned.
        /// If multiple files with the same name are found in a given search path,
        /// an exception is thrown.
        /// </remarks>
        string GetFilePath(string fileName, IEnumerable<string> searchPaths);
    }
}