// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.Framework.CodeGeneration
{
    internal static class ExceptionUtilities
    {
        public static void ValidateStringArgument(string parameterValue, string parameterName)
        {
            if (string.IsNullOrEmpty(parameterValue))
            {
                throw new ArgumentException(String.Format(
                        CultureInfo.CurrentCulture,
                        "Parameter '{0}' cannot be null or empty.",
                        parameterName),
                    parameterName);
            }
        }
    }
}