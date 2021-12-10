// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.DotNet
{
    public class ApplicationInfo : IApplicationInfo
    {
        public ApplicationInfo(string appName, string appBasePath, RoslynWorkspaceHelper workspaceHelper)
            : this(appName, appBasePath, "Debug", workspaceHelper)
        {

        }

        public ApplicationInfo(string appName, string appBasePath, string appConfiguration, RoslynWorkspaceHelper workspaceHelper)
        {
            if (appName == null)
            {
                throw new ArgumentNullException(nameof(appName));
            }
            if (appBasePath == null)
            {
                throw new ArgumentNullException(nameof(appBasePath));
            }
            if (appConfiguration == null)
            {
                throw new ArgumentNullException(nameof(appConfiguration));
            }
            ApplicationName = appName;
            ApplicationBasePath = appBasePath;
            ApplicationConfiguration = appConfiguration;
            WorkspaceHelper = workspaceHelper;
        }

        public string ApplicationBasePath
        {
            get; private set;
        }

        public string ApplicationName
        {
            get; private set;
        }

        public string ApplicationConfiguration
        {
            get; private set;
        }

        public RoslynWorkspaceHelper WorkspaceHelper
        {
            get; private set;
        }
    }
}
