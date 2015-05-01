// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.CodeGeneration.CommandLine
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