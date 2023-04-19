// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public static class ExceptionUtilities
    {
        public static void ValidateStringArgument(string parameterValue, string parameterName)
        {
            if (string.IsNullOrEmpty(parameterValue))
            {
                throw new ArgumentException(String.Format(
                        CultureInfo.CurrentCulture,
                        Resource.NullParamError,
                        parameterName),
                    parameterName);
            }
        }
    }
}
