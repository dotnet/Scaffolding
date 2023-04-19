// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
