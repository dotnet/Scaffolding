// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel
{
    public class Dependency
    {
        public Dependency(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Version = version;
        }
        public string Name { get; }
        public string Version { get; }
    }
}
