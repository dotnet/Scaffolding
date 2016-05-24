using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Test
{
    public class ControllerGeneratorBaseTests
    {
        protected Mock<ILibraryManager> _libraryManager;
        protected Mock<ICodeGeneratorActionsService> _codeGenActionService;
        protected Mock<IServiceProvider> _serviceProvider;
        protected ILogger _logger;
        protected IApplicationInfo _applicationInfo;

        public ControllerGeneratorBaseTests()
        {
            _libraryManager = new Mock<ILibraryManager>();
            _codeGenActionService = new Mock<ICodeGeneratorActionsService>();
            _serviceProvider = new Mock<IServiceProvider>();
            _logger = new ConsoleLogger();
            _applicationInfo = new ApplicationInfo("TestApp", "..", "Debug");
        }

        [Fact]
        public void Test_ValidateNameSpace_ThrowsException()
        {
            var generator = new MockControllerGenerator(
                _libraryManager.Object,
                _applicationInfo,
                _codeGenActionService.Object,
                _serviceProvider.Object,
                _logger
                );

            var model = GetModel();
            model.ControllerNamespace = "Invalid Namespace";
            try
            {
                generator.Generate(model);
                Assert.True(false, "Expected an exception");
            }
            catch(InvalidOperationException ex)
            {
                Assert.Equal("The namespace name 'Invalid Namespace' is not valid.", ex.Message);
                return;
            }
            
        }

        [Fact]
        public void Test_ValidateNameSpace()
        {
            var generator = new MockControllerGenerator(
                _libraryManager.Object,
                _applicationInfo,
                _codeGenActionService.Object,
                _serviceProvider.Object,
                _logger
                );

            var model = GetModel();
            model.ControllerNamespace = "Valid.Namespace";
            try
            {
                generator.Generate(model);
            }
            catch
            {
                Assert.True(false);
                return;
            }

        }

        protected virtual CommandLineGeneratorModel GetModel()
        {
            var model = new CommandLineGeneratorModel();
            model.ControllerName = "TestController";
            return model;
        }
    }

    
}
