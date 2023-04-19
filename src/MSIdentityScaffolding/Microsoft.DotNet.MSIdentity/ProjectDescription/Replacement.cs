// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.MSIdentity.Project
{
    public class Replacement
    {
        public Replacement()
        {
            FilePath = string.Empty;
            ReplaceFrom = string.Empty;
            ReplaceBy = string.Empty;
        }

        public Replacement(string filePath, int index, int length, string replaceFrom, string replaceBy)
        {
            FilePath = filePath;
            Index = index;
            Length = length;
            ReplaceFrom = replaceFrom;
            ReplaceBy = replaceBy;
        }
        public string FilePath { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public string ReplaceFrom { get; set; }
        public string ReplaceBy { get; set; }

        public override string ToString()
        {
            return $"Replace '{ReplaceFrom}' by '{ReplaceBy}'";
        }
    }
}
