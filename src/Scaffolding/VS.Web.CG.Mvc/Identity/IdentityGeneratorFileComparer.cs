// Copyright (c) .NET Foundation. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    internal class IdentityGeneratorFileComparer : IEqualityComparer<IdentityGeneratorFile>
    {
        public bool Equals(IdentityGeneratorFile x, IdentityGeneratorFile y)
        {
            // If they are the same objects or both are null.
            if (x == y)
            {
                return true;
            }

            return string.Equals(x?.Name, y?.Name, StringComparison.Ordinal);

        }

        public int GetHashCode(IdentityGeneratorFile obj)
        {
            return obj?.Name?.GetHashCode() ?? 0;
        }
    }
}
