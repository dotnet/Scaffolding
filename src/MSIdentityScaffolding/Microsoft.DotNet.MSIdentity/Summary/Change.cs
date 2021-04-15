// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DotNet.MSIdentity
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
