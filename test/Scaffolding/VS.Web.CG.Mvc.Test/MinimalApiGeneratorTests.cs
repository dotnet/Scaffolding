using Moq;
using System;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using System.Runtime.InteropServices;
using System.IO;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class MinimalApiGeneratorTests
    {
        private SourceText EndpointsClass = SourceText.From(MsBuildProjectStrings.EndpointsEmptyClass);
        private SourceText ProgramCs = SourceText.From(MsBuildProjectStrings.MinimalProgramcsFile);
        private SourceText CarCs = SourceText.From(MsBuildProjectStrings.CarTxt);
        private const string EndpointsClassName = "Endpoints";
        private IModelTypesLocator modelTypesLocator;
        private AdhocWorkspace workspace;
        private IFileSystem fileSystem;

        [Fact]
        public void GetTemplateNameTests()
        {
            MinimalApiGeneratorCommandLineModel modelWithContext = new MinimalApiGeneratorCommandLineModel
            {
                DataContextClass = "DbContext"
            };

            MinimalApiGeneratorCommandLineModel modelWithoutContext = new MinimalApiGeneratorCommandLineModel
            {
                DataContextClass = string.Empty
            };
            Assert.Equal(Constants.MinimalApiEfNoClassTemplate, MinimalApiGenerator.GetTemplateName(modelWithContext, existingEndpointsFile: true));
            Assert.Equal(Constants.MinimalApiEfTemplate,MinimalApiGenerator.GetTemplateName(modelWithContext, existingEndpointsFile: false));
            Assert.Equal(Constants.MinimalApiNoClassTemplate, MinimalApiGenerator.GetTemplateName(modelWithoutContext, existingEndpointsFile: true));
            Assert.Equal(Constants.MinimalApiTemplate, MinimalApiGenerator.GetTemplateName(modelWithoutContext, existingEndpointsFile: false));
        }

        [Fact]
        public void ValidateModelTests()
        {
            MinimalApiGeneratorCommandLineModel model = new MinimalApiGeneratorCommandLineModel
            {
                EndpintsClassName = "className",
                EndpointsNamespace = "namespaceName"
            };

            MinimalApiGeneratorCommandLineModel invalidClassName = new MinimalApiGeneratorCommandLineModel
            {
                EndpintsClassName = "className ABCD 1234",
                EndpointsNamespace = "namespaceName"
            };

            MinimalApiGeneratorCommandLineModel invalidNamespaceName = new MinimalApiGeneratorCommandLineModel
            {
                EndpintsClassName = "className",
                EndpointsNamespace = "namespaceName  123 %%$%$"
            };

            MinimalApiGeneratorCommandLineModel invalidModel = new MinimalApiGeneratorCommandLineModel
            {
                EndpintsClassName = "$52435i230452////",
                EndpointsNamespace = "using"
            };
            Action modelAction = () => MinimalApiGenerator.ValidateModel(model);
            Action invalidClassNameAction = () => MinimalApiGenerator.ValidateModel(invalidClassName);
            Action invalidNamespaceNameAction = () => MinimalApiGenerator.ValidateModel(invalidNamespaceName);
            Action invalidModelAction = () => MinimalApiGenerator.ValidateModel(invalidModel);
            //assert
            var noException = Record.Exception(modelAction);
            var exception = Assert.Throws<InvalidOperationException>(invalidClassNameAction);
            var exception2 = Assert.Throws<InvalidOperationException>(invalidNamespaceNameAction);
            var exception3 = Assert.Throws<InvalidOperationException>(invalidModelAction);
            //The thrown exception can be used for even more detailed assertions.
            Assert.Null(noException);
            Assert.Contains(string.Format(MessageStrings.InvalidClassName, invalidClassName.EndpintsClassName), exception.Message);
            Assert.Contains(string.Format(MessageStrings.InvalidNamespaceName, invalidNamespaceName.EndpointsNamespace), exception2.Message);
            Assert.Contains(string.Format(MessageStrings.InvalidNamespaceName, invalidModel.EndpointsNamespace), exception3.Message);
            Assert.Contains(string.Format(MessageStrings.InvalidClassName, invalidModel.EndpintsClassName), exception3.Message);
        }

        [Fact]
        public async Task ModifyProgramCsTests()
        {
            var minimalApiGenerator = CreateMinimalApiGenerator();
            var modelType = modelTypesLocator.GetType("Car").FirstOrDefault();
            var endpointsPath = modelTypesLocator.GetAllDocuments().FirstOrDefault(d => d.Name.Contains($"{EndpointsClassName}.cs"));
            var minimalApiModel = new MinimalApiModel(modelType, string.Empty, EndpointsClassName)
            {
                EndpointsName = EndpointsClassName,
                EndpointsNamespace = "MinimalApiTest",
                MethodName = "MapCarEndpoints"
            };
            await minimalApiGenerator.ModifyProgramCs(minimalApiModel);
            string programCsText = fileSystem.ReadAllText("Program.cs");
            Assert.Contains(minimalApiModel.MethodName, programCsText);
        }

        [Fact]
        public void ValidateAndGetOutputPathTests()
        {
            Mock<IApplicationInfo> _mockApp = new Mock<IApplicationInfo>();
            bool isWindows = OperatingSystem.IsWindows();
            var applicationBasePath = Path.Combine("C:", "AppPath");
            _mockApp.Setup(app => app.ApplicationBasePath)
                .Returns(applicationBasePath);
            var minimalApiGenerator = CreateMinimalApiGenerator();
            var minimalCommandline = new MinimalApiGeneratorCommandLineModel
            {
                ModelClass = "testModel",
                EndpintsClassName = "testClass",
                RelativeFolderPath = isWindows ? @"Endpoints\Endpoint" : @"Endpoints/Endpoint"
            };
            var minimalCommandlineWithoutRelativePath = new MinimalApiGeneratorCommandLineModel
            {
                ModelClass = "testModel2",
                EndpintsClassName = "testClass2",
            };

            if (isWindows)
            {
                Assert.Equal(@"C:\AppPath\Endpoints\Endpoint\testClass.cs", minimalApiGenerator.ValidateAndGetOutputPath(minimalCommandline));
                Assert.Equal(@"C:\AppPath\testClass2.cs", minimalApiGenerator.ValidateAndGetOutputPath(minimalCommandlineWithoutRelativePath));
            }
            else
            {
                Assert.Equal(@"C:/AppPath/Endpoints/Endpoint/testClass.cs", minimalApiGenerator.ValidateAndGetOutputPath(minimalCommandline));
                Assert.Equal(@"C:/AppPath/testClass2.cs", minimalApiGenerator.ValidateAndGetOutputPath(minimalCommandlineWithoutRelativePath));
            }
            
        }

        [Fact]
        public async Task AddEndpointsMethodTests()
        {
            var minimalApiGenerator = CreateMinimalApiGenerator();
            var modelType = modelTypesLocator.GetType("Car").FirstOrDefault();
            var endpointsPath = modelTypesLocator.GetAllDocuments().FirstOrDefault(d => d.Name.Contains($"{EndpointsClassName}.cs"));
            var minimalApiModel = new MinimalApiModel(modelType, string.Empty, EndpointsClassName)
            {
                EndpointsName = EndpointsClassName,
                EndpointsNamespace = "MinimalApiTest",
                MethodName = "MapCarEndpoints"
            };
            await minimalApiGenerator.AddEndpointsMethod(MsBuildProjectStrings.EndpointsMethod, endpointsPath.Name, EndpointsClassName, minimalApiModel);
            string endpointsCsText = fileSystem.ReadAllText("Endpoints.cs");
            string programCsText = fileSystem.ReadAllText("Program.cs");
            Assert.Contains(minimalApiModel.MethodName, endpointsCsText);
            Assert.Contains(minimalApiModel.MethodName, programCsText);
        }

        private MinimalApiGenerator CreateMinimalApiGenerator()
        {
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            Mock<IApplicationInfo> appInfo = new Mock<IApplicationInfo>();
            Mock<ILogger> logger = new Mock<ILogger>();
            fileSystem = new MockFileSystem();
            Mock<IProjectContext> projectContext = new Mock<IProjectContext>();
            Mock<IEntityFrameworkService> entityFrameworkService = new Mock<IEntityFrameworkService>();
            Mock<ICodeGeneratorActionsService> codeGeneratorActionsService = new Mock<ICodeGeneratorActionsService>();
            workspace = new AdhocWorkspace();

            var projectInfo = CodeAnalysis.ProjectInfo.Create(ProjectId.CreateNewId(),
                VersionStamp.Default,
                "TestAssembly",
                "TestAssembly",
                LanguageNames.CSharp);

            var project = workspace.AddProject(projectInfo);
            appInfo.Setup(app => app.ApplicationBasePath)
                .Returns(@"C:\AppPath");

            projectContext.Setup(ctx => ctx.AssemblyName)
                .Returns("TestAssembly");

            var id = DocumentId.CreateNewId(project.Id);
            var id2 = DocumentId.CreateNewId(project.Id);
            var id3 = DocumentId.CreateNewId(project.Id);
            var endpointsLoader = TextLoader.From(TextAndVersion.Create(EndpointsClass, VersionStamp.Create()));
            var programCsLoader = TextLoader.From(TextAndVersion.Create(ProgramCs, VersionStamp.Create())); ;
            var carCsLoader = TextLoader.From(TextAndVersion.Create(CarCs, VersionStamp.Create())); ;
            workspace.AddDocument(DocumentInfo.Create(id, "Endpoints.cs", loader: endpointsLoader, filePath: "Endpoints.cs"));
            workspace.AddDocument(DocumentInfo.Create(id2, "Program.cs", loader: programCsLoader, filePath: "Program.cs"));
            workspace.AddDocument(DocumentInfo.Create(id3, "Car.cs", loader: carCsLoader, filePath: "Car.cs"));
            fileSystem.WriteAllText("Program.cs", MsBuildProjectStrings.MinimalProgramcsFile);
            fileSystem.WriteAllText("Endpoints.cs", MsBuildProjectStrings.EndpointsEmptyClass);
            modelTypesLocator = new ModelTypesLocator(workspace);
            return new MinimalApiGenerator(
                appInfo.Object,
                serviceProvider.Object,
                modelTypesLocator,
                logger.Object,
                fileSystem,
                codeGeneratorActionsService.Object,
                projectContext.Object,
                entityFrameworkService.Object,
                workspace);
        }
    }
}
