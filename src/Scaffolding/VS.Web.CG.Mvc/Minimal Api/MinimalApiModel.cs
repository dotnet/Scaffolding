using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi
{
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

        public string EndpointsName { get; set; }
        public bool UseAsync { get; set; }
        public bool NullableEnabled { get; set; }
        public bool OpenAPI { get; set; }
        public string EndpointsNamespace { get; set; }
        public string MethodName { get; set; }

        public IModelMetadata ModelMetadata { get; set; }

        public ModelType ModelType { get; private set; }


        public string ContextTypeName { get; private set; }

        public string ModelTypeName
        {
            get
            {
                return ModelType.Name;
            }
        }

        public string ModelVariable
        {
            get
            {
                return RoslynUtilities.CreateEscapedIdentifier(ModelTypeName.ToLowerInvariantFirstChar());
            }
        }

        public string EntitySetVariable
        {
            get
            {
                return ContextTypeName.ToLowerInvariantFirstChar();
            }
        }

        public string DbContextNamespace { get; private set; }

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

                // Finally we remove the ControllerNamespace as it's not required.
                requiredNamespaces.Remove(EndpointsNamespace);
                return new HashSet<string>(requiredNamespaces);
            }
        }
    }
}
