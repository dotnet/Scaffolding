using System;
using System.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    /// <summary>
    /// Generates syntax tree to add custom attributes to the in-memory assembly
    /// Need to use this for adding the UserSecrets attribute to the assembly.
    /// </summary>
    public class AssemblyAttributeGenerator
    {
        const string attributeTextTemplate = @"[assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute(""{0}"")]";
        Assembly _originalAssembly;
        string assemblyInfoText = @"using System;
using System.Reflection;";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assembly">The original assembly used to check if it has the User secrets attribute</param>
        public AssemblyAttributeGenerator(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            _originalAssembly = assembly;
        }

        /// <summary>
        /// Generates a syntax tree with the custom attribute for User Secrets.
        /// </summary>
        /// <returns></returns>
        internal SyntaxTree GenerateAttributeSyntaxTree()
        {
            if (_originalAssembly.CustomAttributes.Any())
            {
                foreach (var attr in _originalAssembly.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == "Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute")
                    {
                        var attributeText = string.Format(attributeTextTemplate, attr.ConstructorArguments.First().Value);
                        assemblyInfoText = $"{assemblyInfoText}{Environment.NewLine}{attributeText}";
                    }
                }
            }

            return CSharpSyntaxTree.ParseText(assemblyInfoText);
        }
    }
}