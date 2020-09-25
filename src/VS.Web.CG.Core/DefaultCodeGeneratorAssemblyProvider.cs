// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class DefaultCodeGeneratorAssemblyProvider : ICodeGeneratorAssemblyProvider
    {
        //we need this assembly to get the ICodeGenerators and templates. 
        //instead of looking assemblies referencing "Microsoft,VisualStudio.Web.CodeGeneration",
        //we just try to find "Microsoft.VisualStudio.Web.CodeGenerators.Mvc."
        private static readonly HashSet<string> _codeGenerationFrameworkAssemblies =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Microsoft.VisualStudio.Web.CodeGenerators.Mvc",
            };

        private readonly ICodeGenAssemblyLoadContext _assemblyLoadContext;
        private IProjectContext _projectContext;
         
        public DefaultCodeGeneratorAssemblyProvider(IProjectContext projectContext, ICodeGenAssemblyLoadContext loadContext)
        {
            if(loadContext == null)
            {
                throw new ArgumentNullException(nameof(loadContext));
            }
            if(projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }
            _projectContext = projectContext;
            _assemblyLoadContext = loadContext;

        }

        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {
                var list = _codeGenerationFrameworkAssemblies
                    .Where(assembly => _projectContext.GetAssembly(assembly) != null);
                    
                return list.Select(lib => _assemblyLoadContext.LoadFromName(new AssemblyName(lib)));
            }
        }
    }
}