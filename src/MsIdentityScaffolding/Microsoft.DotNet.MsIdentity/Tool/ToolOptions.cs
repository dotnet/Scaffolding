// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DotNet.MsIdentity
{
    public class ToolOptions
    {
        public bool ListAADApps { get; set; }
        public bool ListServicePrincipals { get; set; }
        public bool ProvisionApp { get; set; }
        public bool ValidateAppParams { get; set; }
    }
}