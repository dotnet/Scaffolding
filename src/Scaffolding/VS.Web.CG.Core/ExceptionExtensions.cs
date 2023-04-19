// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public static class ExceptionExtensions
    {
        public static Exception Unwrap(this Exception ex, ILogger logger = null)
        {
            if (ex == null)
            {
                return null;
            }

            var inner = ex;
            if (logger != null)
            {
                logger.LogMessage($"{inner.Message} StackTrace:{Environment.NewLine}{inner.StackTrace}{Environment.NewLine}");
            }

            while (inner.InnerException != null)
            {
                inner = inner.InnerException;
                if (logger != null)
                {
                    logger.LogMessage($"{inner.Message} StackTrace:{Environment.NewLine}{inner.StackTrace}{Environment.NewLine}");
                }
            }

            return inner;
        }
    }
}
