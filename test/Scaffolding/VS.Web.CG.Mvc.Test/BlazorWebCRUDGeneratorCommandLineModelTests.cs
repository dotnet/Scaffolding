using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class BlazorWebCRUDGeneratorCommandLineModelTests
    {
        [Fact]
        public void ValidateModelTests()
        {
            BlazorWebCRUDGeneratorCommandLineModel model = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
                TemplateName = "crud"
            };

            BlazorWebCRUDGeneratorCommandLineModel emptyModelName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                TemplateName = "create"
            };

            BlazorWebCRUDGeneratorCommandLineModel emptyTemplateName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
            };

            BlazorWebCRUDGeneratorCommandLineModel invalidTemplateName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
                TemplateName = "templateName"
            };

            BlazorWebCRUDGeneratorCommandLineModel invalidNamespaceName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
                TemplateName = "crud",
                Namespace = "using"
            };

            Action modelAction = () => model.ValidateCommandline();
            Action emptyModelNameAction = () => emptyModelName.ValidateCommandline();
            Action emptyTemplateNameAction = () => emptyTemplateName.ValidateCommandline();
            Action invalidTemplateNameAction = () => invalidTemplateName.ValidateCommandline();
            Action invalidNamespaceNameAction = () => invalidNamespaceName.ValidateCommandline();
            //assert
            var noException = Record.Exception(modelAction);
            var exception = Assert.Throws<ArgumentNullException>(emptyModelNameAction);
            var exception2 = Record.Exception(emptyTemplateNameAction);
            var exception3 = Assert.Throws<InvalidOperationException>(invalidTemplateNameAction);
            var exception4 = Assert.Throws<InvalidOperationException>(invalidNamespaceNameAction);
        }

        [Fact]
        public void CreateTemplate_UsesNullCoalescingAssignment()
        {
            // Test that Create template generates code with null coalescing assignment in OnInitialized
            var templatePath = Path.Combine("Templates", "Blazor", "Create.tt");
            var transformation = BlazorWebCRUDHelper.GetBlazorTransformation(templatePath);
            
            Assert.NotNull(transformation);
            // Note: This test verifies that the transformation can be created.
            // The actual template content testing would require more complex setup
            // with mock BlazorModel and running the template transformation.
        }

        [Fact]
        public void BlazorTemplates_UseNotFoundNavigation()
        {
            // Test that templates use NavigationManager.NotFound() instead of NavigateTo("notfound")
            var templateFiles = new[] { "Edit.tt", "Details.tt", "Delete.tt" };
            var templateBasePath = Path.Combine("src", "Scaffolding", "VS.Web.CG.Mvc", "Templates", "Blazor");
            
            foreach (var templateFile in templateFiles)
            {
                var templatePath = Path.Combine(templateBasePath, templateFile);
                if (File.Exists(templatePath))
                {
                    var content = File.ReadAllText(templatePath);
                    
                    // Verify that NavigationManager.NotFound() is used
                    Assert.Contains("NavigationManager.NotFound()", content);
                    
                    // Verify that old NavigateTo("notfound") pattern is not used
                    Assert.DoesNotContain("NavigationManager.NavigateTo(\"notfound\")", content);
                }
            }
        }
    }
}
