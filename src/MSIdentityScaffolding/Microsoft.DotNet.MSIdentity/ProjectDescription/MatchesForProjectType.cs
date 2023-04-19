// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.MSIdentity.Project
{
    public class MatchesForProjectType
    {
        public string? FileRelativePath
        {
            get { return _fileRelativePath?.Replace("\\", Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)); }
            set { _fileRelativePath = value; }
        }
        private string? _fileRelativePath;

        public string[]? MatchAny { get; set; }

        public string? Sets { get; set; }

        public string? FolderRelativePath { get; set; }

        public string? FileExtension { get; set; }

        /// <summary>
        /// Either FileRelativePath is defined, along with MatchAny
        /// Or FolderRelativePath is defined
        /// </summary>
        /// <returns></returns>
        internal bool IsValid()
        {
            bool isValid =
                !string.IsNullOrEmpty(FileRelativePath) || !string.IsNullOrEmpty(FolderRelativePath)
                && (string.IsNullOrEmpty(FileRelativePath) || (MatchAny != null && MatchAny.Any()));
            return isValid;
        }
    }
}
