using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework
{
    public class RelatedModelMetadata
    {
        public string AssociationPropertyName { get; set; }

        public string DisplayPropertyName { get; set; }

        public string EntitySetName { get; set; }

        public string[] FoeignKeyPropertyNames { get; set; }

        public string[] PrimaryKeyNames { get; set; }

        public string ShortTypeName { get; set; }

        public string TypeName { get; set; }
    }
}
