// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class Dependency
    {
        public Dependency(string name, string version, string itemSpec)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNullOrEmpty(itemSpec, nameof(itemSpec));
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (Version == null ? 0 : Version.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var other = obj as Dependency;
            return other != null && other.Name == this.Name && other.Version == this.Version;
        }
    }
}
