// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            if(string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
