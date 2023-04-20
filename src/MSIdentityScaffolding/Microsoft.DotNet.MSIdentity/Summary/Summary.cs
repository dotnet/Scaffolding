// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.MSIdentity
{
    public class Summary
    {
        public List<Change> changes { get; } = new List<Change>();
    }
}
