using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Razor
{
    public class RazorPageWithContextTemplateModel : RazorPageGeneratorTemplateModel
    {
        public RazorPageWithContextTemplateModel(ModelType modelType, string dbContextFullTypeName)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (dbContextFullTypeName == null)
            {
                throw new ArgumentNullException(nameof(dbContextFullTypeName));
            }

            ModelType = modelType;

            var classNameModel = new ClassNameModel(dbContextFullTypeName);

            ContextTypeName = classNameModel.ClassName;
            DbContextNamespace = classNameModel.NamespaceName;
        }

        public bool UseSqlite { get; set; }
        
        public string ViewDataTypeName { get; set; }

        public string ViewDataTypeShortName { get; set; }

        public string ContextTypeName { get; set; }
        public string DbContextNamespace { get; private set; }

        public ModelType ModelType { get; private set; }

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

        public HashSet<string> RequiredNamespaces
        {
            get
            {
                var requiredNamespaces = new SortedSet<string>(StringComparer.Ordinal);
                // We add PageModel Namespace first to make other entries not added to the set if they match.
                requiredNamespaces.Add(NamespaceName);

                var modelTypeNamespace = ModelType.Namespace;

                if (!string.IsNullOrWhiteSpace(modelTypeNamespace))
                {
                    requiredNamespaces.Add(modelTypeNamespace);
                }

                if (!string.IsNullOrWhiteSpace(DbContextNamespace))
                {
                    requiredNamespaces.Add(DbContextNamespace);
                }

                // Finally we remove the PageModel Namespace as it's not required.
                requiredNamespaces.Remove(NamespaceName);
                return new HashSet<string>(requiredNamespaces);
            }
        }
    }
}
