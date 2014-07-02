// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.CodeGeneration
{
    /// <summary>
    /// Specifies a required command line argument.
    /// </summary>
    public class CommandArgument
    {
        public CommandArgument([NotNull]string name)
            :this(name, description: "")
        {
        }

        public CommandArgument([NotNull]string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        internal string Value { get; set; }
    }
}