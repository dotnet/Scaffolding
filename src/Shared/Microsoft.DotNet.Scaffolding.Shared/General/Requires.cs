// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils
{
    public static class Requires
    {
        public static void NotNull(object o, string name)
        {
            if (o == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void NotNullOrEmpty(string s, string name)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
