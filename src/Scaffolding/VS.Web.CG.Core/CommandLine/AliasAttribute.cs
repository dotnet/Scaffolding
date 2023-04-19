// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration.CommandLine
{
    /// <summary>
    /// Specifies a command line alias for a code generator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AliasAttribute : Attribute
    {
        public AliasAttribute(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Alias = alias;
        }

        public string Alias { get; private set; }
    }
}
