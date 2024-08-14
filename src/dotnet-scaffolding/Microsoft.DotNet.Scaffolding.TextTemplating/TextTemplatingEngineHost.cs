// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.VisualStudio.TextTemplating;

namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
///     This is an internal API that supports the T4 Templating infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new scaffolding release.
///     Referencing from dotnet/efcore/src/EFCore.Design/Scaffolding/Internal/TextTemplatingEngineHost.cs
/// </summary>
internal class TextTemplatingEngineHost : ITextTemplatingSessionHost, ITextTemplatingEngineHost, IServiceProvider
{
    private static readonly List<string> _noWarn = ["CS1701", "CS1702"];

    private readonly IServiceProvider? _serviceProvider;
    private ITextTemplatingSession? _session;
    private CompilerErrorCollection? _errors;
    private string? _extension;
    private Encoding? _outputEncoding;
    private bool _fromOutputDirective;

    public TextTemplatingEngineHost(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
    }

    [AllowNull]
    public virtual ITextTemplatingSession Session
    {
        get => _session ??= CreateSession();
        set => _session = value;
    }

    public virtual IList<string> StandardAssemblyReferences { get; } = new[]
    {
        typeof(ITextTemplatingEngineHost).Assembly.Location, typeof(CompilerErrorCollection).Assembly.Location
    };

    public virtual IList<string> StandardImports { get; } = new[] { "System" };

    public virtual string? TemplateFile { get; set; }

    public virtual string Extension
        => _extension ?? ".cs";

    public virtual CompilerErrorCollection Errors
        => _errors ??= [];

    public virtual Encoding OutputEncoding
        => _outputEncoding ?? Encoding.UTF8;

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

    public virtual object? GetHostOption(string optionName)
        => null;

    public virtual bool LoadIncludeText(string requestFileName, out string content, out string location)
    {
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
        if (Path.IsPathRooted(assemblyReference))
        {
            return assemblyReference;
        }

        var path = DependencyContext.Default?.CompileLibraries
            .FirstOrDefault(l => l.Assemblies.Any(a => Path.GetFileNameWithoutExtension(a) == assemblyReference))
            ?.ResolveReferencePaths()
            .First(p => Path.GetFileNameWithoutExtension(p) == assemblyReference);
        if (path is not null)
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
        => throw new FileNotFoundException($"Failed to resolve type for directive processor {processorName}");

    public virtual string ResolveParameterValue(string directiveId, string processorName, string parameterName)
        => string.Empty;

    public virtual string ResolvePath(string path)
        => !Path.IsPathRooted(path) && Path.IsPathRooted(TemplateFile)
            ? Path.Combine(Path.GetDirectoryName(TemplateFile)!, path)
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

    public virtual object? GetService(Type serviceType)
        => _serviceProvider?.GetService(serviceType);
}
