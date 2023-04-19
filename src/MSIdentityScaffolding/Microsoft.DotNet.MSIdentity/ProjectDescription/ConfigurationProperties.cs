// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.MSIdentity.Project
{
    public class ConfigurationProperties
    {
        public string? FileRelativePath
        {
            get { return _fileRelativePath?.Replace("\\", Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)); }
            set { _fileRelativePath = value; }
        }
        private string? _fileRelativePath;

        public PropertyMapping[] Properties { get; set; } = S_emptyPropertyMappings;
        public static PropertyMapping[] S_emptyPropertyMappings { get; set; } = new PropertyMapping[0];

        public override string? ToString()
        {
            return FileRelativePath;
        }

        public bool IsValid()
        {
            bool valid = !string.IsNullOrEmpty(FileRelativePath)
                && !Properties.Any(p => !p.IsValid());
            return valid;
        }
    }
}
