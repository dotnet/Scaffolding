// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models
{
    internal class EntraIdModel
    {
        public ProjectInfo? ProjectInfo { get; set; }
        public string? Username { get; set; }
        public string? TenantId { get; set; }
        public string? Application { get; set; }
        public string? SelectApplication { get; set; }
        public string? BaseOutputPath { get; set; }
        public string? EntraIdNamespace { get; set; }
    }
}
