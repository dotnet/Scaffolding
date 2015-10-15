using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.EntityFramework;

namespace Microsoft.Extensions.CodeGenerators.Mvc
{
    public class ModelTypeAndContextModel
    {
        public ModelType ModelType { get; set; }

        public ModelMetadata ModelMetadata { get; set; }

        public string DbContextFullName { get; set; }
    }
}
