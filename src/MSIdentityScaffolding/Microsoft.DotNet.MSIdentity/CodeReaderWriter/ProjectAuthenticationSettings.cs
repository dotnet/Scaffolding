// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Project;

namespace Microsoft.DotNet.MSIdentity.CodeReaderWriter
{
    public class ProjectAuthenticationSettings
    {
        public ProjectAuthenticationSettings(ProjectDescription? projectDescription = null)
        {
            ProjectDescription = projectDescription;
        }

        public ApplicationParameters ApplicationParameters { get; } = new ApplicationParameters();

        public List<Replacement> Replacements { get; } = new List<Replacement>();

        public ProjectDescription? ProjectDescription { get; private set; }
    }
}
