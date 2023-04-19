// Copyright (c) .NET Foundation. All rights reserved.

using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration.CommandLine
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
