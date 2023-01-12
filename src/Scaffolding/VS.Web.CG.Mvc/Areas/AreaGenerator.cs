using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Areas
{
    [Alias("area")]
    public class AreaGenerator : ICodeGenerator
    {
        private static readonly string[] AreaFolders = new string[]
        {
            "Controllers",
            "Models",
            "Data",
            "Views"
        };

        private IServiceProvider _serviceProvider { get; set; }
        private IApplicationInfo _appInfo { get; set; }
        private ILogger _logger { get; set; }
        private IModelTypesLocator _modelTypesLocator { get; set; }

        public AreaGenerator(IApplicationInfo applicationInfo,
            IServiceProvider serviceProvider,
            IModelTypesLocator modelTypesLocator,
            ILogger logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo));
            _modelTypesLocator = modelTypesLocator ?? throw new ArgumentNullException(nameof(modelTypesLocator));
        }

        public async Task GenerateCode(AreaGeneratorCommandLine model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            EnsureFolderLayout(model);

            var readmeGenerator = ActivatorUtilities.CreateInstance<ReadMeGenerator>(_serviceProvider);
            try
            {
                await readmeGenerator.GenerateReadmeForArea();
            }
            catch (Exception ex)
            {
                _logger.LogMessage(string.Format(MessageStrings.ReadmeGenerationFailed, ex.Message));
                throw ex.Unwrap(_logger);
            }
        }

        /// <summary>
        /// Creates a folder hierarchy:
        ///     ProjectDir
        ///        \ Areas
        ///            \ AreaName
        ///                \ Controllers
        ///                \ Data
        ///                \ Models
        ///                \ Views
        /// </summary>
        private void EnsureFolderLayout(AreaGeneratorCommandLine model)
        {
            var areaBasePath = Path.Combine(_appInfo.ApplicationBasePath, "Areas");
            if (!Directory.Exists(areaBasePath))
            {
                Directory.CreateDirectory(areaBasePath);
            }

            var areaPath = Path.Combine(areaBasePath, model.Name);
            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            foreach (var areaFolder in AreaFolders)
            {
                var path = Path.Combine(areaPath, areaFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }
    }
}
