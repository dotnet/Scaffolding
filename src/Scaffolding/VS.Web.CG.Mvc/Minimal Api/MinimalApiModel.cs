using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi
{
    /// <summary>
    /// Model for Razor templates to create or add to existing Endpoints file for the 'dotnet-aspnet-codegenerator minimalapi' scenario 
    /// </summary>
    public class MinimalApiModel
    {
        public MinimalApiModel(
           ModelType modelType,
           string dbContextFullTypeName,
           string endpointsFullTypeName)
        {
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

            if (string.IsNullOrEmpty(endpointsFullTypeName))
            {
                throw new ArgumentNullException(nameof(endpointsFullTypeName));
            }
            EndpointsName = endpointsFullTypeName;
            ModelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
        }

        //Endpoints class name
        public string EndpointsName { get; set; }
        public bool NullableEnabled { get; set; }

        //If CRUD endpoints support Open API
        public bool OpenAPI { get; set; }

        //Sqlite for sqlite and mac/linux scenarios.
        public bool UseSqlite { get; set; }

        //Use TypedResults for minimal apis.
        public bool UseTypedResults { get; set; }

        //Generated namespace for a Endpoints class/file. If using an existing file, does not apply.
        public string EndpointsNamespace { get; set; }

        //Method name for the new static method with the CRUD endpoints.
        public string MethodName { get; set; }

        //Holds Model and EF Context metadata.
        public IModelMetadata ModelMetadata { get; set; }

        //Model info
        public ModelType ModelType { get; private set; }

        //DbContext class' name
        public string ContextTypeName { get; private set; }

        //Model class' name
        public string ModelTypeName
        {
            get
            {
                return ModelType.Name;
            }
        }

        //Model class' name but lower case
        public string ModelVariable
        {
            get
            {
                return RoslynUtilities.CreateEscapedIdentifier(ModelTypeName.ToLowerInvariantFirstChar());
            }
        }

        //variable name holding the models in the DbContext class
        public string EntitySetVariable
        {
            get
            {
                return ContextTypeName.ToLowerInvariantFirstChar();
            }
        }

        public string DbContextNamespace { get; private set; }

        //namespaces to add in Endpoints file.
        public HashSet<string> RequiredNamespaces
        {
            get
            {
                var requiredNamespaces = new SortedSet<string>(StringComparer.Ordinal);
                // We add ControllerNamespace first to make other entries not added to the set if they match.
                requiredNamespaces.Add(EndpointsNamespace);

                var modelTypeNamespace = ModelType.Namespace;

                if (!string.IsNullOrWhiteSpace(modelTypeNamespace))
                {
                    requiredNamespaces.Add(modelTypeNamespace);
                }

                if (!string.IsNullOrWhiteSpace(DbContextNamespace))
                {
                    requiredNamespaces.Add(DbContextNamespace);
                }

                if (OpenAPI)
                {
                    requiredNamespaces.Add("Microsoft.AspNetCore.OpenApi");
                }

                if (UseTypedResults)
                {
                    requiredNamespaces.Add("Microsoft.AspNetCore.Http.HttpResults");
                }

                // Finally we remove the ControllerNamespace as it's not required.
                requiredNamespaces.Remove(EndpointsNamespace);
                return new HashSet<string>(requiredNamespaces);
            }
        }
    }
}
