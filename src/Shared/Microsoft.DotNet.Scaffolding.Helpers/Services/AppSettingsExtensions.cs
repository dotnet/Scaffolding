// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services
{
    public static class AppSettingsExtensions
    {
        public static WorkspaceSettings Workspace(this IAppSettings settings)
        {
            return settings.GetSettings<WorkspaceSettings>("workspace") ?? new WorkspaceSettings();
        }

        public static T? GetSettings<T>(this IAppSettings settings, string sectionName) where T : class
        {
            var sectionObject = settings.GetSettings(sectionName);

            return sectionObject as T;
        }
    }
}
