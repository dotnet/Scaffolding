// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Versioning;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class DotNetSdkResolver
    {
        private readonly string _installationPath;

        public DotNetSdkResolver()
            : this(Path.GetDirectoryName(new Muxer().MuxerPath))
        { }

        public DotNetSdkResolver(string installationDir)
        {
            _installationPath = installationDir;
        }

        /// <summary>
        /// Find the latest SDK installation (according to SemVer 1.0)
        /// </summary>
        /// <returns>Path to SDK root directory</returns>
        public string ResolveLatest()
        {
            return Installed.Select(d => new { path = d, version = SemanticVersion.Parse(Path.GetFileName(d)) })
                .OrderByDescending(sdk => sdk.version)
                .First()
                .path;
        }

        // TODO resolve from the version selected in global.json
        // public string ResolveProjectSdk();

        public IEnumerable<string> Installed
            => Directory.EnumerateDirectories(Path.Combine(_installationPath, "sdk"));
    }
}