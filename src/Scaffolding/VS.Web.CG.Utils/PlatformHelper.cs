// Copyright (c) .NET Foundation. All rights reserved.

using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public static class PlatformHelper
    {
        private static readonly Lazy<bool> _isMono = new Lazy<bool>(() => Type.GetType("Mono.Runtime") != null);

        public static bool IsMono
        {
            get
            {
                return _isMono.Value;
            }
        }
    }
}
