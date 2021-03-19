// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DotNet.MsIdentity
{
    public class Change
    {
        public Change(string description)
        {
            Description = description;
        }

        public string Description { get; set; }
    }
}
