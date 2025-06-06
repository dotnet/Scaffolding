// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings
{
    internal class EntraIdSettings
    {
        public string? Username { get; set; }
        public string? Project { get; set; }
        public string? TenantId { get; set; }
        public string? Application { get; set; }
        public string? SelectApplication { get; set; }
    }


}
