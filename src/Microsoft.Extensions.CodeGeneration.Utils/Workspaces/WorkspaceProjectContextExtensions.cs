// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.ProjectModel.Workspaces
{
    public static class WorkspaceProjectContextExtensions
    {
        public static Workspace CreateWorkspace(this ProjectContext context)
        {
            return new ProjectJsonWorkspace(context);
        }
    }
}