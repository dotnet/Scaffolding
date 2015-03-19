// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.Framework.CodeGeneration.Templating.Compilation
{
    public class MetadataReferencesProvider
    {
        private List<MetadataReference> _references = new List<MetadataReference>();

        public List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();
            references.Add(MetadataReference.CreateFromAssembly(
                Assembly.Load(new AssemblyName("mscorlib"))));
            references.Add(MetadataReference.CreateFromAssembly(
                Assembly.Load(new AssemblyName("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))));
            references.Add(MetadataReference.CreateFromAssembly(
                Assembly.Load(new AssemblyName("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))));
            references.Add(MetadataReference.CreateFromAssembly(
                Assembly.Load(new AssemblyName("Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"))));
            references.Add(MetadataReference.CreateFromAssembly(typeof(RazorTemplateBase).GetTypeInfo().Assembly));

            return references;
        }
    }
}