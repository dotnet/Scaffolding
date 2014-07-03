// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.CodeGeneration.CommandLine
{
    /// <summary>
    /// Indicates a command line argument.
    /// Arguments are passed as positional parameters from the command line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ArgumentAttribute : Attribute
    {
        /// <summary>
        /// Help string shown for the argument.
        /// </summary>
        public string Description { get; set; }
    }
}