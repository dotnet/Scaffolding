// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration.CommandLine
{
    /// <summary>
    /// Indicates a command line option.
    /// Options are passed as named parameters from the command line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class OptionAttribute : Attribute
    {
        /// <summary>
        /// Name of the option. Usually passed with the prefix --.
        /// If the Name is not set, name of the property on which the attribute
        /// is declared is assumed.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// ShortName for convenience. Usually passed with the prefix -.
        /// </summary>
        public string ShortName { get; set; }
        /// <summary>
        /// Help string shown for the option.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// An optional default value to set if user did not pass a value
        /// for this option.
        /// </summary>
        public object DefaultValue { get; set; }
    }
}
