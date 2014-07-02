// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.CodeGeneration
{
    public class CodeGeneratorMetadata
    {
        public CodeGeneratorMetadata([NotNull]Type type)
            :this(type,
                  name: type.FullName,
                  arguments: Enumerable.Empty<CommandArgument>(),
                  options: Enumerable.Empty<CommandOption>())
        {
        }

        public CodeGeneratorMetadata([NotNull]Type type,
            [NotNull]string name,
            [NotNull]IEnumerable<CommandArgument> arguments,
            [NotNull]IEnumerable<CommandOption> options)
        {
            Type = type;
            Name = name;
            Arguments = arguments;
            Options = options;
        }

        public Type Type { get; set; }

        public string Name { get; private set; }

        public IEnumerable<CommandArgument> Arguments { get; private set; }

        public IEnumerable<CommandOption> Options { get; private set; }
    }
}