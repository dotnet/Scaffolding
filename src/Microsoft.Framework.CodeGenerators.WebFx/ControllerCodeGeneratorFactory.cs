// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.CodeGeneration;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    public class ControllerCodeGeneratorFactory : CodeGeneratorFactory
    {
        private static CodeGeneratorMetadata _codeGeneratorMetadata = new CodeGeneratorMetadata(
            type: typeof(ControllerCodeGenerator),
            name: "controller",
            arguments: new List<CommandArgument>()
            {
                new CommandArgument("model", "Model class to be used"),
                new CommandArgument("dataContext", "data context to be used"),
            },
            options: new List<CommandOption>
            {
                new CommandOption("generateViews", CommandOptionType.Switch),
                new CommandOption("referenceScriptLibraries", CommandOptionType.Switch),
            });

        public ControllerCodeGeneratorFactory()
            : base(_codeGeneratorMetadata)
        {
        }
    }
}