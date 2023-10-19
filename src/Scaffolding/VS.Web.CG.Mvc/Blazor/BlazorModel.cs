// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor
{
    /// <summary>
    /// Model for Razor templates to create or add to existing Endpoints file for the 'dotnet-aspnet-codegenerator minimalapi' scenario 
    /// </summary>
    public class BlazorModel
    {
        public BlazorModel(
           ModelType modelType,
           string dbContextFullTypeName)
        {
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
            if (string.IsNullOrEmpty(dbContextFullTypeName))
            {
                DbContextNamespace = string.Empty;
                ContextTypeName = string.Empty;
            }
            else
            {
                var classNameModel = new ClassNameModel(dbContextFullTypeName);
                DbContextNamespace = classNameModel.NamespaceName;
                ContextTypeName = classNameModel.ClassName;
            }
        }

        //Database type eg. SQL Server, SQLite, Cosmos DB, Postgres and more later.
        public DbProvider DatabaseProvider { get; set; }

        //Generated namespace for a Endpoints class/file. If using an existing file, does not apply.
        public string Namespace { get; set; }

        //Holds Model and EF Context metadata.
        public IModelMetadata ModelMetadata { get; set; }

        //Model info
        public ModelType ModelType { get; private set; }

        //DbContext class' name
        public string ContextTypeName { get; private set; }

        //Model class' name
        public string ModelTypeName => ModelType.Name;
            
        //Model class' name but lower case
        public string ModelVariable => RoslynUtilities.CreateEscapedIdentifier(ModelTypeName.ToLowerInvariantFirstChar());

        //variable name holding the models in the DbContext class
        public string EntitySetVariable => ContextTypeName.ToLowerInvariantFirstChar();

        public string DbContextNamespace { get; private set; }
        public string Template { get; set; }

        //namespaces to add in Endpoints file.
        public HashSet<string> RequiredNamespaces
        {
            get
            {
                var requiredNamespaces = new SortedSet<string>(StringComparer.Ordinal)
                {
                    // We add ControllerNamespace first to make other entries not added to the set if they match.
                    Namespace
                };

                var modelTypeNamespace = ModelType.Namespace;

                if (!string.IsNullOrWhiteSpace(modelTypeNamespace))
                {
                    requiredNamespaces.Add(modelTypeNamespace);
                }

                if (!string.IsNullOrWhiteSpace(DbContextNamespace))
                {
                    requiredNamespaces.Add(DbContextNamespace);
                }

                return new HashSet<string>(requiredNamespaces);
            }
        }
    }
}
