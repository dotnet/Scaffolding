// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Project;
using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
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
