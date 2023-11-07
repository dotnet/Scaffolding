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

        //Generated namespace for the model being used.
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

        public Dictionary<string, string> _inputTypeDict;
        //used for Create page for the Blazor CRUD scenario
        public Dictionary<string, string> InputTypeDict
        {
            get
            {
                if (_inputTypeDict == null)
                {
                    _inputTypeDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "string", "InputText" },
                        { "DateTime", "InputDate" },
                        { "double", "InputNumber" },
                        { "int", "InputNumber" },
                        { "bool", "InputCheckbox" }
                    };
                }

                return _inputTypeDict;
            }
        }

        public Dictionary<string, string> _inputClassDict;
        //used for Create page for the Blazor CRUD scenario
        public Dictionary<string, string> InputClassDict
        {
            get
            {
                if (_inputClassDict == null)
                {
                    _inputClassDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "string", "form-control" },
                        { "DateTime", "form-control" },
                        { "double", "form-control" },
                        { "int", "form-control" },
                        { "bool", "form-check-input" }
                    };
                }

                return _inputClassDict;
            }
        }

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
