// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.CodeGenerators;

namespace Microsoft.Extensions.CodeGeneration.Templating
{
    internal class RazorTemplatingHost : RazorEngineHost
    {
        private static readonly string[] _defaultNamespaces = new[]
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Dynamic",
            "Microsoft.Extensions.CodeGeneration",
            "Microsoft.Extensions.CodeGeneration.Templating",
        };

        public RazorTemplatingHost(Type baseType)
            : base(new CSharpRazorCodeLanguage())
        {
            if (baseType == null)
            {
                throw new ArgumentNullException(nameof(baseType));
            }

            DefaultBaseClass = baseType.FullName;

            //ToDo: Why Do I need templateTypeName? Do I need other parameters?
            GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: "ExecuteAsync",
                writeMethodName: "Write",
                writeLiteralMethodName: "WriteLiteral",
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "",
                generatedTagHelperContext: new GeneratedTagHelperContext())
            {
            };

            foreach (var ns in _defaultNamespaces)
            {
                NamespaceImports.Add(ns);
            }
        }
    }
}