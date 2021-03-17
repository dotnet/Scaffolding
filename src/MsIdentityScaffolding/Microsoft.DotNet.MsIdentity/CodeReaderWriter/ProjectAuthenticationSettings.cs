// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.DotNet.MsIdentity.AuthenticationParameters;
using Microsoft.DotNet.MsIdentity.Project;
using System.Collections.Generic;

namespace Microsoft.DotNet.MsIdentity.CodeReaderWriter
{
    public class ProjectAuthenticationSettings
    {
        public ProjectAuthenticationSettings(ProjectDescription projectDescription)
        {
            ProjectDescription = projectDescription;
        }

        public ApplicationParameters ApplicationParameters { get; } = new ApplicationParameters();

        public List<Replacement> Replacements { get; } = new List<Replacement>();

        public ProjectDescription ProjectDescription { get; private set; }
    }
}
