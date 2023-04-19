﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.MSIdentity.Project
{
    public class PropertyMapping
    {
        /// <summary>
        /// Path to the property.
        /// </summary>
        public string? Property { get; set; }

        /// <summary>
        /// Represented authentication property.
        /// </summary>
        public string? Represents { get; set; }

        public string[]? MatchAny { get; set; }
        public string? Default { get; set; }

        /// <summary>
        /// Which flag is set?
        /// </summary>
        public string? Sets { get; set; }

        public override string? ToString()
        {
            return Property;
        }

        public bool IsValid()
        {
            return (!string.IsNullOrEmpty(Property) && !string.IsNullOrEmpty(Represents))
                 || !string.IsNullOrEmpty(Sets);
        }
    }
}
