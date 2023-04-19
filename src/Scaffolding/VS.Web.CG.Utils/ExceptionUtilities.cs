// Copyright (c) .NET Foundation. All rights reserved.

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
