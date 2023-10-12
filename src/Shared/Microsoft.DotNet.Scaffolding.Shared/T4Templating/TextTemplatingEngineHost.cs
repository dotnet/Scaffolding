using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.VisualStudio.TextTemplating;

namespace Microsoft.DotNet.Scaffolding.Shared.T4Templating
{
    /// <summary>
    /// Enables callers to obtain an object denoting the current session.
    /// A session represents series of executions of text templates.
    /// The session object can be used to pass information from the host into the code of the text template.
    /// </summary>
    public class TextTemplatingEngineHost : ITextTemplatingSessionHost, ITextTemplatingEngineHost, IServiceProvider
    {
        private static readonly List<string> _noWarn = new List<string>()
        {
            "CS1701",
            "CS1702"
        };

        private readonly IServiceProvider _serviceProvider;
        private ITextTemplatingSession _session;
        private CompilerErrorCollection _errors;
        private string _extension;
        private Encoding _outputEncoding;
        private bool _fromOutputDirective;

        public TextTemplatingEngineHost(IServiceProvider serviceProvider = null)
            => _serviceProvider = serviceProvider;

        public virtual ITextTemplatingSession Session
        {
            get => _session ??= CreateSession();
            set => _session = value;
        }

        public virtual IList<string> StandardAssemblyReferences { get; } = new string[]
        {
            typeof(ITextTemplatingEngineHost).Assembly.Location,
            typeof(CompilerErrorCollection).Assembly.Location
        };

        public virtual IList<string> StandardImports { get; } = new[]
        {
            "System"
        };

        public virtual string TemplateFile { get; set; }

        public virtual string Extension
            => _extension ?? ".cs";

        public virtual CompilerErrorCollection Errors
            => _errors ??= new CompilerErrorCollection();

        public virtual Encoding OutputEncoding
            => _outputEncoding ??= Encoding.UTF8;

        public virtual void Initialize()
        {
            _session?.Clear();
            _errors = null;
            _extension = null;
            _outputEncoding = null;
            _fromOutputDirective = false;
        }

        public virtual ITextTemplatingSession CreateSession()
            => new TextTemplatingSession();

        public virtual object GetHostOption(string optionName)
            => null;

        public virtual bool LoadIncludeText(string requestFileName, out string content, out string location)
        {
            // TODO: Expand variables?
            location = ResolvePath(requestFileName);
            var exists = File.Exists(location);
            content = exists
                ? File.ReadAllText(location)
                : string.Empty;

            return exists;
        }

        public virtual void LogErrors(CompilerErrorCollection errors)
            => Errors.AddRange(errors.Cast<CompilerError>().Where(e => !_noWarn.Contains(e.ErrorNumber)).ToArray());

        public virtual AppDomain ProvideTemplatingAppDomain(string content)
            => AppDomain.CurrentDomain;

        public virtual string ResolveAssemblyReference(string assemblyReference)
        {
            var path = DependencyContext.Default?.CompileLibraries
                .FirstOrDefault(l => l.Assemblies.Any(a => Path.GetFileNameWithoutExtension(a) == assemblyReference))
                ?.ResolveReferencePaths()
                .First(p => Path.GetFileNameWithoutExtension(p) == assemblyReference);

            if (path != null)
            {
                return path;
            }

            try
            {
                return Assembly.Load(assemblyReference).Location;
            }
            catch
            {
            }

            return assemblyReference;
        }

        public virtual Type ResolveDirectiveProcessor(string processorName)
            => throw new FileNotFoundException("bingobongo");

        public virtual string ResolveParameterValue(string directiveId, string processorName, string parameterName)
            => string.Empty;

        public virtual string ResolvePath(string path)
            => !Path.IsPathRooted(path) && Path.IsPathRooted(TemplateFile)
                ? Path.Combine(Path.GetDirectoryName(TemplateFile), path)
                : path;

        public virtual void SetFileExtension(string extension)
            => _extension = extension;

        public virtual void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
            if (_fromOutputDirective)
            {
                return;
            }

            _outputEncoding = encoding;
            _fromOutputDirective = fromOutputDirective;
        }

        public virtual object GetService(Type serviceType)
            => _serviceProvider?.GetService(serviceType);
    }

}
