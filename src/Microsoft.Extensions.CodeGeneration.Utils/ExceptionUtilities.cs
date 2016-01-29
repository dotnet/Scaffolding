// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.Extensions.CodeGeneration
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