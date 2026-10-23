// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.DotNet.Scaffolding.Shared.T4Templating;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using ConsoleLogger = Microsoft.DotNet.MSIdentity.Shared.ConsoleLogger;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor
{
    [Alias("blazor-identity")]
    public class BlazorIdentityGenerator : ICodeGenerator
    {
        private IApplicationInfo AppInfo { get; set; }
        private ILogger Logger { get; set; }
        private IModelTypesLocator ModelTypesLocator { get; set; }
        private IFileSystem FileSystem { get; set; }
        private IFilesLocator FileLocator { get; set; }
        private IProjectContext ProjectContext { get; set; }
        private Workspace Workspace { get; set; }
        private ConsoleLogger ConsoleLogger { get; set; }
        private ICodeGenAssemblyLoadContext AssemblyLoadContextLoader { get; set; }
        private const string Main = nameof(Main);
        private static readonly StringComparer PathComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        private static readonly StringComparison PathComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        private bool CalledFromCommandline => !(FileSystem is SimulationModeFileSystem);

        private IEnumerable<string> _templateFolders;
        //Folders where the .tt templates for Blazor Identity scenario live. Should be in VS.Web.CG.Mvc\Templates\BlazorIdentity
        private IEnumerable<string> TemplateFolders
        {
            get
            {
                if (_templateFolders is null)
                {
                    _templateFolders = TemplateFoldersUtilities.GetTemplateFolders(
                        containingProject: Constants.ThisAssemblyName,
                        applicationBasePath: AppInfo.ApplicationBasePath,
                        baseFolders: new[] { "BlazorIdentity", "General" },
                        projectContext: ProjectContext);
                }
                return _templateFolders;
            }
        }
        private IDictionary<string, string> _allBlazorIdentityFiles;
        private IDictionary<string, string> AllBlazorIdentityFiles
        {
            get
            {
                if (_allBlazorIdentityFiles is null)
                {
                    _allBlazorIdentityFiles = BlazorIdentityHelper.GetBlazorIdentityFiles(FileSystem, TemplateFolders);
                }

                return _allBlazorIdentityFiles;
            }
        }

        private IEnumerable<string> _generalT4Files;
        private IEnumerable<string> GeneralT4Files
        {
            get
            {
                if (_generalT4Files is null)
                {
                    _generalT4Files = BlazorIdentityHelper.GetGeneralT4Files(FileSystem, TemplateFolders);
                }

                return _generalT4Files;
            }
        }

        private IReadOnlyList<(string TemplateFolder, string FullPath)> _allBlazorIdentityStaticFiles;
        private IReadOnlyList<(string TemplateFolder, string FullPath)> AllBlazorIdentityStaticFiles
        {
            get => _allBlazorIdentityStaticFiles ??= [.. BlazorIdentityHelper.GetBlazorIdentityStaticFiles(FileSystem, TemplateFolders)];
        }

        private IList<Type> _blazorIdentityTemplateTypes;
        private IList<Type> BlazorIdentityTemplateTypes
        {
            get
            {
                if (_blazorIdentityTemplateTypes is null)
                {
                    var allTypes = Assembly.GetExecutingAssembly().GetTypes();
                    _blazorIdentityTemplateTypes = allTypes.Where(t => t.FullName.Contains("Mvc.Templates.BlazorIdentity")).ToList();
                }

                return _blazorIdentityTemplateTypes;
            }
        }

        public BlazorIdentityGenerator(IApplicationInfo applicationInfo,
            IModelTypesLocator modelTypesLocator,
            ILogger logger,
            IFileSystem fileSystem,
            IFilesLocator fileLocator,
            IProjectContext projectContext,
            IEntityFrameworkService entityframeworkService,
            ICodeGenAssemblyLoadContext loader,
            Workspace workspace)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            AppInfo = applicationInfo ?? throw new ArgumentNullException(nameof(applicationInfo)); ;
            ModelTypesLocator = modelTypesLocator ?? throw new ArgumentNullException(nameof(modelTypesLocator));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            FileLocator = fileLocator ?? throw new ArgumentNullException(nameof(fileLocator));
            ProjectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            ConsoleLogger = new ConsoleLogger(jsonOutput: false);
            AssemblyLoadContextLoader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>
        /// Scaffold API Controller code into the provided (or created) Endpoints file. If no DbContext is provided, we will use the non-EF templates.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task GenerateCode(BlazorIdentityCommandLineModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (model.ListFiles)
            {
                ShowFileList();
                return;
            }

            var blazorTemplateModel = await ValidateAndBuild(model);
            ExecuteTemplates(blazorTemplateModel);
            AddStaticFiles(blazorTemplateModel);
            AddReadmeFile(blazorTemplateModel.BaseOutputPath);
            await ModifyFilesAsync(blazorTemplateModel);
        }

        internal void AddReadmeFile(string outputPath)
        {
            string fileName = BlazorIdentityHelper.BlazorIdentityReadmeFileName;
            string readmeFilePath = Path.Combine(outputPath, fileName);
            FileSystem.WriteAllText(readmeFilePath, BlazorIdentityHelper.BlazorIdentityReadmeString);
            Logger.LogMessage($"Added Blazor identity file : {fileName}");
        }

        internal async Task<BlazorIdentityModel> ValidateAndBuild(BlazorIdentityCommandLineModel commandlineModel)
        {
            commandlineModel.DatabaseProvider = ModelMetadataUtilities.ValidateDatabaseProvider(commandlineModel.DatabaseProviderString, Logger, isIdentity: true);
            ValidateRequiredDependencies(commandlineModel.DatabaseProvider);
            if (string.IsNullOrEmpty(commandlineModel.RootNamespace) || RoslynUtilities.IsValidNamespace(commandlineModel.RootNamespace))
            {
                Logger.LogMessage("Empty or invalid namespace provided, using default namespace.");
                commandlineModel.RootNamespace = ProjectContext.RootNamespace;
            }

            var rootIdentityNamespace = $"{commandlineModel.RootNamespace}.Components.Account";
            // MainLayout can live in the server project, a WebAssembly client, or a referenced shared Razor project.
            var layoutNamespace = ResolveLayoutNamespace(
                ProjectContext.ProjectName,
                commandlineModel.RootNamespace,
                AppInfo.ApplicationBasePath,
                FileSystem,
                ProjectContext.ProjectReferenceInformation,
                ProjectContext.ProjectReferences);
            var defaultDbContextNamespace = $"{commandlineModel.RootNamespace}.Data";
            var defaultUserNamespace = $"{commandlineModel.RootNamespace}.Data";
            var blazorIdentityModel = new BlazorIdentityModel
            {
                BlazorIdentityNamespace = rootIdentityNamespace,
                BlazorLayoutNamespace = layoutNamespace
            };

            var createDbContext = false;
            //if DataContext is specified, try to find or create it.
            if (!string.IsNullOrEmpty(commandlineModel.DataContextClass))
            {
                var existingDbContext = await RoslynWorkspaceHelper.FindExistingType(
                    Workspace,
                    AssemblyLoadContextLoader,
                    ProjectContext.AssemblyName,
                    commandlineModel.DataContextClass);
                // We need to create one with what the user specified.
                if (existingDbContext is null)
                {
                    blazorIdentityModel.DbContextName = IdentityHelper.GetClassNameFromTypeName(commandlineModel.DataContextClass);
                    blazorIdentityModel.DbContextNamespace = IdentityHelper.GetNamespaceFromTypeName(commandlineModel.DataContextClass)
                        ?? defaultDbContextNamespace;
                    blazorIdentityModel.DatabaseProvider = commandlineModel.DatabaseProvider;
                    createDbContext = true;
                }
                else
                {
                    IdentityHelper.ValidateExistingDbContext(existingDbContext, commandlineModel.UserClass);
                    var userClassType = IdentityHelper.FindUserTypeFromDbContext(existingDbContext);
                    blazorIdentityModel.UserClassName = userClassType.Name;
                    blazorIdentityModel.UserClassNamespace = userClassType.Namespace;
                    blazorIdentityModel.DbContextName = existingDbContext.Name;
                    blazorIdentityModel.DbContextNamespace = existingDbContext.Namespace;
                    blazorIdentityModel.DatabaseProvider = commandlineModel.DatabaseProvider;
                }
            }
            else
            {
                // --dbContext paramter was not specified. So we need to generate one using convention.
                blazorIdentityModel.DbContextName = IdentityHelper.GetDefaultDbContextName(ProjectContext.ProjectName);
                blazorIdentityModel.DbContextNamespace = defaultDbContextNamespace;
                blazorIdentityModel.DatabaseProvider = commandlineModel.DatabaseProvider;
                createDbContext = true;
            }

            var createUserClass = false;
            // if an existing user class was determined from the DbContext, don't try to get it from here.
            // Identity scaffolding must use the user class tied to the existing DbContext (when there is one).
            if (string.IsNullOrEmpty(blazorIdentityModel.UserClassName))
            {
                if (string.IsNullOrEmpty(commandlineModel.UserClass))
                {
                    //check for ApplicationUser (defaulkt IdentityUser for apps with existing blazor identity)
                    var existingApplicationUser = await RoslynWorkspaceHelper.FindExistingType(
                        Workspace,
                        AssemblyLoadContextLoader,
                        ProjectContext.AssemblyName,
                        "ApplicationUser");
                    if (existingApplicationUser is null)
                    {
                        //default IdentityUser shipped with "Microsoft.AspNetCore.Identity
                        blazorIdentityModel.UserClassName = "IdentityUser";
                        blazorIdentityModel.UserClassNamespace = "Microsoft.AspNetCore.Identity";
                    }
                    else
                    {
                        blazorIdentityModel.UserClassName = existingApplicationUser.Name;
                        blazorIdentityModel.UserClassNamespace = existingApplicationUser.Namespace;
                    }
                }
                else
                {
                    var existingUser = await RoslynWorkspaceHelper.FindExistingType(
                        Workspace,
                        AssemblyLoadContextLoader,
                        ProjectContext.AssemblyName,
                        commandlineModel.UserClass);

                    if (existingUser != null)
                    {
                        IdentityHelper.ValidateExistingUserType(existingUser);
                        blazorIdentityModel.UserClassName = existingUser.Name;
                        blazorIdentityModel.UserClassNamespace = existingUser.Namespace;
                    }
                    else
                    {
                        createUserClass = true;
                        blazorIdentityModel.UserClassName = IdentityHelper.GetClassNameFromTypeName(commandlineModel.UserClass);
                        blazorIdentityModel.UserClassNamespace = IdentityHelper.GetNamespaceFromTypeName(commandlineModel.UserClass)
                            ?? defaultDbContextNamespace;
                    }
                }
            }

            var applicationUserModel = new IdentityApplicationUserModel
            {
                UserClassName = blazorIdentityModel.UserClassName,
                UserClassNamespace = blazorIdentityModel.UserClassNamespace
            };

            if (createUserClass)
            {
                ExecuteApplicationUserTemplate(applicationUserModel);
            }

            if (createDbContext)
            {
                var dbContextModel = new IdentityDbContextModel
                {
                    DbContextName = blazorIdentityModel.DbContextName,
                    DbContextNamespace = blazorIdentityModel.DbContextNamespace,
                    UserClassModel = applicationUserModel
                };

                ExecuteDbContextTemplate(dbContextModel);
                var connectionStringsWriter = new ConnectionStringsWriter(AppInfo, FileSystem);
                connectionStringsWriter.AddConnectionString(dbContextModel.DbContextName, dbContextModel.DbContextName, commandlineModel.DatabaseProvider);
            }

            //gather blazor identity files to generate
            if (!string.IsNullOrEmpty(commandlineModel.Files))
            {
                blazorIdentityModel.FilesToGenerate = commandlineModel.Files.Split(IdentityHelper.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else if (!string.IsNullOrEmpty(commandlineModel.ExcludeFiles))
            {
                IEnumerable<string> excludedFiles = commandlineModel.ExcludeFiles.Split(IdentityHelper.SemicolonSeparator, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
                //validate excluded files
                var errors = new List<string>();
                var invalidFiles = excludedFiles.Where(f => !AllBlazorIdentityFiles.ContainsKey(f));
                if (invalidFiles.Any())
                {
                    errors.Add(MessageStrings.InvalidFilesListMessage);
                    errors.AddRange(invalidFiles);
                }

                if (errors.Count != 0)
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
                }

                //get files to overwrite
                blazorIdentityModel.FilesToGenerate = AllBlazorIdentityFiles.Keys.Except(excludedFiles).ToList();
            }
            else
            {
                blazorIdentityModel.FilesToGenerate = AllBlazorIdentityFiles.Keys.ToList();
            }

            blazorIdentityModel.BaseOutputPath = Path.Combine(AppInfo.ApplicationBasePath, commandlineModel.RelativeFolderPath);
            return blazorIdentityModel;
        }

        internal static string ResolveLayoutNamespace(
            string projectName,
            string rootNamespace,
            string applicationBasePath,
            IFileSystem fileSystem,
            IEnumerable<ProjectReferenceInformation> projectReferences,
            IEnumerable<string> directProjectReferences)
        {
            projectName = string.IsNullOrEmpty(projectName) ? rootNamespace : projectName;
            var mainLayoutInServerProject = Path.Combine(applicationBasePath, "Components", "Layout", "MainLayout.razor");
            if (fileSystem.FileExists(mainLayoutInServerProject))
            {
                var razorNamespace = ResolveRazorNamespace(
                    mainLayoutInServerProject,
                    applicationBasePath,
                    fileSystem);
                return $"{razorNamespace ?? $"{rootNamespace}.Components.Layout"}.MainLayout";
            }

            var directReferencePaths = new HashSet<string>(PathComparer);
            foreach (var directProjectReference in directProjectReferences ?? Enumerable.Empty<string>())
            {
                directReferencePaths.Add(Path.GetFullPath(directProjectReference));
                directReferencePaths.Add(Path.GetFullPath(Path.Combine(applicationBasePath, directProjectReference)));
            }
            var clientProjectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                $"{projectName}.Client"
            };
            if (projectName.EndsWith(".Web", StringComparison.OrdinalIgnoreCase))
            {
                clientProjectNames.Add($"{projectName.Substring(0, projectName.Length - ".Web".Length)}.Client");
            }

            var layoutCandidates = new List<LayoutCandidate>();
            foreach (var projectReference in projectReferences ?? Enumerable.Empty<ProjectReferenceInformation>())
            {
                if (!directReferencePaths.Contains(Path.GetFullPath(projectReference.FullPath)))
                {
                    continue;
                }

                var projectDirectory = Path.GetDirectoryName(projectReference.FullPath);
                if (string.IsNullOrEmpty(projectDirectory) || !fileSystem.DirectoryExists(projectDirectory))
                {
                    continue;
                }

                if (!projectReference.IsRazorProject)
                {
                    continue;
                }

                var razorComponentPaths = new HashSet<string>(
                    (projectReference.RazorComponentItems ?? new List<string>())
                        .Select(path => Path.IsPathRooted(path)
                            ? Path.GetFullPath(path)
                            : Path.GetFullPath(Path.Combine(projectDirectory, path))),
                    PathComparer);
                var mainLayoutPaths = new[]
                {
                    Path.Combine(projectDirectory, "Components", "Layout", "MainLayout.razor"),
                    Path.Combine(projectDirectory, "Layout", "MainLayout.razor")
                }.Where(path => fileSystem.FileExists(path) &&
                    razorComponentPaths.Contains(Path.GetFullPath(path)));
                foreach (var mainLayoutPath in mainLayoutPaths)
                {
                    var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, mainLayoutPath));
                    var namespaceSuffix = string.IsNullOrEmpty(relativeDirectory) || relativeDirectory == "."
                        ? string.Empty
                        : $".{relativeDirectory.Replace(Path.DirectorySeparatorChar, '.')}";
                    var referencedRootNamespace = string.IsNullOrEmpty(projectReference.RootNamespace)
                        ? projectReference.ProjectName
                        : projectReference.RootNamespace;
                    var razorNamespace = ResolveRazorNamespace(mainLayoutPath, projectDirectory, fileSystem);
                    var isClientProject = clientProjectNames.Contains(projectReference.ProjectName);

                    layoutCandidates.Add(new LayoutCandidate
                    {
                        ComponentPath = Path.GetFullPath(mainLayoutPath),
                        Namespace = $"{razorNamespace ?? $"{referencedRootNamespace}{namespaceSuffix}"}.MainLayout",
                        ProjectPath = Path.GetFullPath(projectReference.FullPath),
                        IsClientProject = isClientProject
                    });
                }
            }

            var clientCandidates = layoutCandidates
                .Where(candidate => candidate.IsClientProject)
                .GroupBy(GetLayoutCandidateIdentity, PathComparer)
                .Select(group => group.First())
                .ToList();
            if (clientCandidates.Count == 1)
            {
                return clientCandidates[0].Namespace;
            }
            if (clientCandidates.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple MainLayout components were found in referenced client projects: {FormatLayoutCandidates(clientCandidates)}");
            }

            var sharedCandidates = layoutCandidates
                .GroupBy(GetLayoutCandidateIdentity, PathComparer)
                .Select(group => group.First())
                .ToList();
            if (sharedCandidates.Count == 1)
            {
                return sharedCandidates[0].Namespace;
            }
            if (sharedCandidates.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple MainLayout components were found in referenced projects: {FormatLayoutCandidates(sharedCandidates)}");
            }

            return $"{rootNamespace}.Client.Layout.MainLayout";
        }

        private static string GetLayoutCandidateIdentity(LayoutCandidate candidate)
        {
            return $"{candidate.ProjectPath}\0{candidate.ComponentPath}";
        }

        private static string FormatLayoutCandidates(IEnumerable<LayoutCandidate> candidates)
        {
            return string.Join(
                ", ",
                candidates.Select(candidate =>
                    $"{candidate.Namespace} ({candidate.ComponentPath}, project: {candidate.ProjectPath})"));
        }

        private static string ResolveRazorNamespace(
            string componentPath,
            string projectDirectory,
            IFileSystem fileSystem)
        {
            var razorNamespace = ReadRazorNamespace(componentPath, fileSystem);
            if (!string.IsNullOrEmpty(razorNamespace))
            {
                return razorNamespace;
            }

            var currentDirectory = Path.GetDirectoryName(componentPath);
            while (!string.IsNullOrEmpty(currentDirectory) &&
                   currentDirectory.StartsWith(projectDirectory, PathComparison))
            {
                var importsPath = Path.Combine(currentDirectory, "_Imports.razor");
                if (fileSystem.FileExists(importsPath))
                {
                    razorNamespace = ReadRazorNamespace(importsPath, fileSystem);
                    if (!string.IsNullOrEmpty(razorNamespace))
                    {
                        return razorNamespace + GetNamespaceSuffix(
                            currentDirectory,
                            Path.GetDirectoryName(componentPath));
                    }
                }

                if (string.Equals(currentDirectory, projectDirectory, PathComparison))
                {
                    break;
                }
                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }

            return null;
        }

        private static string GetNamespaceSuffix(string importsDirectory, string componentDirectory)
        {
            var relativeDirectory = Path.GetRelativePath(importsDirectory, componentDirectory);
            if (string.IsNullOrEmpty(relativeDirectory) || relativeDirectory == ".")
            {
                return string.Empty;
            }

            var namespaceSegments = relativeDirectory.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);
            return namespaceSegments.Length == 0
                ? string.Empty
                : $".{string.Join(".", namespaceSegments)}";
        }

        private static string ReadRazorNamespace(string path, IFileSystem fileSystem)
        {
            const string namespaceDirective = "@namespace";
            var contents = fileSystem.ReadAllText(path);
            if (string.IsNullOrEmpty(contents))
            {
                return null;
            }

            return RemoveRazorComments(contents)
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line =>
                    line.StartsWith(namespaceDirective, StringComparison.Ordinal) &&
                    line.Length > namespaceDirective.Length &&
                    char.IsWhiteSpace(line[namespaceDirective.Length]))
                .Select(line => line.Substring(namespaceDirective.Length).Trim())
                .FirstOrDefault(namespaceValue => !string.IsNullOrEmpty(namespaceValue));
        }

        private static string RemoveRazorComments(string contents)
        {
            var result = new StringBuilder(contents.Length);
            for (var index = 0; index < contents.Length; index++)
            {
                if (index + 1 < contents.Length &&
                    contents[index] == '@' &&
                    contents[index + 1] == '@')
                {
                    result.Append("@@");
                    index++;
                    continue;
                }

                if (index + 1 < contents.Length &&
                    contents[index] == '@' &&
                    contents[index + 1] == '*')
                {
                    var commentEnd = contents.IndexOf("*@", index + 2, StringComparison.Ordinal);
                    var endIndex = commentEnd < 0 ? contents.Length : commentEnd + 2;
                    AppendWhitespacePreservingLines(result, contents, index, endIndex);
                    index = endIndex - 1;
                    continue;
                }

                if (contents[index] == '@')
                {
                    var codeEnd = FindEmbeddedCSharpEnd(contents, index);
                    if (codeEnd > index)
                    {
                        AppendWhitespacePreservingLines(result, contents, index, codeEnd);
                        index = codeEnd - 1;
                        continue;
                    }
                }

                result.Append(contents[index]);
            }

            return result.ToString();
        }

        private static int FindEmbeddedCSharpEnd(string contents, int transitionIndex)
        {
            var codeIndex = transitionIndex + 1;
            if (codeIndex >= contents.Length)
            {
                return transitionIndex;
            }

            if (contents[codeIndex] == '{')
            {
                return FindMatchingCSharpDelimiter(
                    contents,
                    codeIndex,
                    SyntaxKind.OpenBraceToken,
                    SyntaxKind.CloseBraceToken);
            }

            if (contents[codeIndex] == '(')
            {
                return FindMatchingCSharpDelimiter(
                    contents,
                    codeIndex,
                    SyntaxKind.OpenParenToken,
                    SyntaxKind.CloseParenToken);
            }

            if (!SyntaxFacts.IsIdentifierStartCharacter(contents[codeIndex]))
            {
                return transitionIndex;
            }

            var identifierEnd = codeIndex + 1;
            while (identifierEnd < contents.Length &&
                   SyntaxFacts.IsIdentifierPartCharacter(contents[identifierEnd]))
            {
                identifierEnd++;
            }

            var identifier = contents.Substring(codeIndex, identifierEnd - codeIndex);
            if (string.Equals(identifier, "code", StringComparison.Ordinal) ||
                string.Equals(identifier, "functions", StringComparison.Ordinal))
            {
                var blockStart = SkipWhitespace(contents, identifierEnd);
                return blockStart < contents.Length && contents[blockStart] == '{'
                    ? FindMatchingRazorCodeBlockEnd(contents, blockStart)
                    : transitionIndex;
            }

            if (IsRazorControlBlock(identifier))
            {
                var conditionStart = SkipWhitespace(contents, identifierEnd);
                if (conditionStart < contents.Length && contents[conditionStart] == '(')
                {
                    var conditionEnd = FindMatchingCSharpDelimiter(
                        contents,
                        conditionStart,
                        SyntaxKind.OpenParenToken,
                        SyntaxKind.CloseParenToken);
                    var blockStart = SkipWhitespace(contents, conditionEnd);
                    if (blockStart < contents.Length && contents[blockStart] == '{')
                    {
                        var blockEnd = FindMatchingRazorCodeBlockEnd(contents, blockStart);
                        return string.Equals(identifier, "if", StringComparison.Ordinal)
                            ? FindIfChainEnd(contents, blockEnd)
                            : blockEnd;
                    }
                    return conditionEnd;
                }
            }

            if (string.Equals(identifier, "namespace", StringComparison.Ordinal))
            {
                return transitionIndex;
            }

            var expression = SyntaxFactory.ParseExpression(contents.Substring(codeIndex));
            var firstToken = expression.GetFirstToken();
            return !firstToken.IsMissing &&
                   firstToken.SpanStart == 0 &&
                   string.Equals(firstToken.ValueText, identifier, StringComparison.Ordinal)
                ? codeIndex + expression.Span.End
                : transitionIndex;
        }

        private static bool IsRazorControlBlock(string identifier)
        {
            return string.Equals(identifier, "if", StringComparison.Ordinal) ||
                string.Equals(identifier, "for", StringComparison.Ordinal) ||
                string.Equals(identifier, "foreach", StringComparison.Ordinal) ||
                string.Equals(identifier, "while", StringComparison.Ordinal) ||
                string.Equals(identifier, "switch", StringComparison.Ordinal) ||
                string.Equals(identifier, "lock", StringComparison.Ordinal) ||
                string.Equals(identifier, "using", StringComparison.Ordinal);
        }

        private static int SkipWhitespace(string contents, int startIndex)
        {
            while (startIndex < contents.Length && char.IsWhiteSpace(contents[startIndex]))
            {
                startIndex++;
            }

            return startIndex;
        }

        private static int FindMatchingCSharpDelimiter(
            string contents,
            int openDelimiterIndex,
            SyntaxKind openDelimiter,
            SyntaxKind closeDelimiter)
        {
            var depth = 0;
            foreach (var token in SyntaxFactory.ParseTokens(contents.Substring(openDelimiterIndex)))
            {
                if (token.IsKind(openDelimiter))
                {
                    depth++;
                }
                else if (token.IsKind(closeDelimiter))
                {
                    depth--;
                    if (depth == 0)
                    {
                        return openDelimiterIndex + token.Span.End;
                    }
                }
            }

            return contents.Length;
        }

        private static int FindMatchingRazorCodeBlockEnd(string contents, int openBraceIndex)
        {
            var depth = 0;
            for (var index = openBraceIndex; index < contents.Length; index++)
            {
                var literalEnd = FindCSharpLiteralEnd(contents, index);
                if (literalEnd > index)
                {
                    index = literalEnd - 1;
                    continue;
                }

                if (index + 1 < contents.Length &&
                    contents[index] == '/' &&
                    contents[index + 1] == '/')
                {
                    var lineEnd = contents.IndexOfAny(new[] { '\r', '\n' }, index + 2);
                    index = lineEnd < 0 ? contents.Length - 1 : lineEnd - 1;
                    continue;
                }

                if (index + 1 < contents.Length &&
                    contents[index] == '/' &&
                    contents[index + 1] == '*')
                {
                    var commentEnd = contents.IndexOf("*/", index + 2, StringComparison.Ordinal);
                    index = commentEnd < 0 ? contents.Length - 1 : commentEnd + 1;
                    continue;
                }

                if (index + 1 < contents.Length &&
                    contents[index] == '@' &&
                    contents[index + 1] == '*')
                {
                    var commentEnd = contents.IndexOf("*@", index + 2, StringComparison.Ordinal);
                    index = commentEnd < 0 ? contents.Length - 1 : commentEnd + 1;
                    continue;
                }

                if (contents[index] == '<' && IsRazorMarkupStart(contents, index))
                {
                    var markupEnd = FindRazorMarkupEnd(contents, index);
                    if (markupEnd > index)
                    {
                        index = markupEnd - 1;
                        continue;
                    }
                }

                if (contents[index] == '{')
                {
                    depth++;
                }
                else if (contents[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return index + 1;
                    }
                }
            }

            return contents.Length;
        }

        private static int FindIfChainEnd(string contents, int blockEnd)
        {
            var currentEnd = blockEnd;
            while (currentEnd < contents.Length)
            {
                var elseStart = SkipWhitespace(contents, currentEnd);
                if (!IsKeywordAt(contents, elseStart, "else"))
                {
                    return currentEnd;
                }

                var next = SkipWhitespace(contents, elseStart + "else".Length);
                if (IsKeywordAt(contents, next, "if"))
                {
                    var conditionStart = SkipWhitespace(contents, next + "if".Length);
                    if (conditionStart >= contents.Length || contents[conditionStart] != '(')
                    {
                        return currentEnd;
                    }

                    var conditionEnd = FindMatchingCSharpDelimiter(
                        contents,
                        conditionStart,
                        SyntaxKind.OpenParenToken,
                        SyntaxKind.CloseParenToken);
                    var blockStart = SkipWhitespace(contents, conditionEnd);
                    if (blockStart >= contents.Length || contents[blockStart] != '{')
                    {
                        return currentEnd;
                    }

                    currentEnd = FindMatchingRazorCodeBlockEnd(contents, blockStart);
                    continue;
                }

                if (next >= contents.Length || contents[next] != '{')
                {
                    return currentEnd;
                }

                return FindMatchingRazorCodeBlockEnd(contents, next);
            }

            return currentEnd;
        }

        private static bool IsKeywordAt(string contents, int index, string keyword)
        {
            if (index < 0 ||
                index + keyword.Length > contents.Length ||
                !contents.AsSpan(index, keyword.Length).SequenceEqual(keyword.AsSpan()))
            {
                return false;
            }

            return index + keyword.Length == contents.Length ||
                !SyntaxFacts.IsIdentifierPartCharacter(contents[index + keyword.Length]);
        }

        private static int FindCSharpLiteralEnd(string contents, int index)
        {
            var current = contents[index];
            if (current != '"' &&
                current != '\'' &&
                current != '$' &&
                (current != '@' ||
                    index + 1 >= contents.Length ||
                    (contents[index + 1] != '"' && contents[index + 1] != '$')))
            {
                return index;
            }

            var expression = SyntaxFactory.ParseExpression(contents.Substring(index));
            return expression.SpanStart == 0 &&
                (expression is LiteralExpressionSyntax || expression is InterpolatedStringExpressionSyntax)
                ? index + expression.Span.End
                : index;
        }

        private static bool IsRazorMarkupStart(string contents, int index)
        {
            if (index + 1 >= contents.Length)
            {
                return false;
            }

            var next = contents[index + 1];
            return char.IsLetter(next) || next == '!' || next == '?';
        }

        private static int FindRazorMarkupEnd(string contents, int markupStart)
        {
            var depth = 0;
            var index = markupStart;
            while (index < contents.Length)
            {
                var tagStart = contents.IndexOf('<', index);
                if (tagStart < 0 || tagStart + 1 >= contents.Length)
                {
                    return markupStart;
                }

                if (contents.AsSpan(tagStart).StartsWith("<!--".AsSpan(), StringComparison.Ordinal))
                {
                    var commentEnd = contents.IndexOf("-->", tagStart + 4, StringComparison.Ordinal);
                    if (commentEnd < 0)
                    {
                        return contents.Length;
                    }
                    index = commentEnd + 3;
                    continue;
                }

                var isClosingTag = contents[tagStart + 1] == '/';
                var tagEnd = FindMarkupTagEnd(contents, tagStart + 1);
                if (tagEnd < 0)
                {
                    return markupStart;
                }

                var isDeclaration = contents[tagStart + 1] == '!' || contents[tagStart + 1] == '?';
                var isSelfClosing = tagEnd > tagStart && contents[tagEnd - 1] == '/';
                if (!isDeclaration)
                {
                    if (isClosingTag)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            return tagEnd + 1;
                        }
                    }
                    else if (!isSelfClosing)
                    {
                        depth++;
                    }
                    else if (depth == 0)
                    {
                        return tagEnd + 1;
                    }
                }

                index = tagEnd + 1;
            }

            return markupStart;
        }

        private static int FindMarkupTagEnd(string contents, int index)
        {
            var quote = '\0';
            for (; index < contents.Length; index++)
            {
                if (quote != '\0')
                {
                    if (contents[index] == quote)
                    {
                        quote = '\0';
                    }
                    continue;
                }

                if (contents[index] == '"' || contents[index] == '\'')
                {
                    quote = contents[index];
                }
                else if (contents[index] == '>')
                {
                    return index;
                }
            }

            return -1;
        }

        private static void AppendWhitespacePreservingLines(
            StringBuilder result,
            string contents,
            int startIndex,
            int endIndex)
        {
            for (var index = startIndex; index < endIndex; index++)
            {
                result.Append(contents[index] == '\r' || contents[index] == '\n'
                    ? contents[index]
                    : ' ');
            }
        }

        private sealed class LayoutCandidate
        {
            public string ComponentPath { get; set; }

            public bool IsClientProject { get; set; }

            public string Namespace { get; set; }

            public string ProjectPath { get; set; }
        }

        private void ShowFileList()
        {
            Logger.LogMessage("File List:\n");
            IEnumerable<string> files = AllBlazorIdentityFiles.Keys;
            Logger.LogMessage(string.Join(Environment.NewLine, files));
            if (FileSystem is SimulationModeFileSystem simModefileSystem)
            {
                foreach (string fileName in files)
                {
                    simModefileSystem.AddMetadataMessage(fileName);
                }
            }
        }

        private void ValidateRequiredDependencies(DbProvider dbProvider)
        {
            var dbProviderPackage = EfConstants.EfPackagesDict[dbProvider] ?? string.Empty;
            var dependencies = new HashSet<string>()
            {
                EfConstants.AspnetNetCoreIdentityEfPackageName,
                EfConstants.EfToolsPackageName,
                dbProviderPackage
            };

            var missingPackages = dependencies.Where(d => !ProjectContext.PackageDependencies.Any(p => p.Name.Equals(d, StringComparison.OrdinalIgnoreCase)));
            if (CalledFromCommandline && missingPackages.Any())
            {
                throw new InvalidOperationException(
                    string.Format(MessageStrings.InstallPackagesForScaffoldingIdentity, string.Join(",", missingPackages)));
            }
        }

        internal async Task ModifyFilesAsync(BlazorIdentityModel blazorIdentityModel)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "blazorIdentityChanges.json";
            var jsonText = ProjectModelHelper.GetManifestResource(assembly, resourceName);
            CodeModifierConfig minimalApiChangesConfig = null;
            try
            {
                minimalApiChangesConfig = JsonSerializer.Deserialize<CodeModifierConfig>(jsonText);
            }
            catch (JsonException ex)
            {
                Logger.LogMessage($"Error deserializing {resourceName}. {ex.Message}");
            }

            //Getting Program.cs document
            var programCsFile = minimalApiChangesConfig.Files.FirstOrDefault(x => x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
            await ModifyProgramCsAsync(programCsFile, blazorIdentityModel);

            //Modify all other non .cs files
            var otherFiles = minimalApiChangesConfig.Files.Where(x => !x.FileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            var projectPath = Path.GetDirectoryName(ProjectContext.ProjectFullPath);
            var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
            foreach (var file in otherFiles)
            {
                var fileDoc = project.GetDocumentFromName(file.FileName, FileSystem);
                await DocumentBuilder.ApplyTextReplacements(file, fileDoc, new CodeChangeOptions(), FileSystem);
                if (file.FileName.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase) &&
                    FileSystem.FileExists(fileDoc.Name))
                {
                    var usingStatement = $"@using {blazorIdentityModel.BlazorIdentityNamespace}.Shared";
                    var fileText = FileSystem.ReadAllText(fileDoc.Name);
                    if (!fileText.Contains(usingStatement, StringComparison.OrdinalIgnoreCase))
                    {
                        fileText = usingStatement + Environment.NewLine + fileText;
                        FileSystem.WriteAllText(fileDoc.Name, fileText.ToString());
                        Logger.LogMessage($"Modified {fileDoc.Name}.\n");
                    }
                }
            }
        }

        internal async Task ModifyProgramCsAsync(CodeFile programCsFile, BlazorIdentityModel blazorIdentityModel)
        {
            if (programCsFile is null)
            {
                return;
            }

            var programType = ModelTypesLocator.GetType("<Program>$").FirstOrDefault() ?? ModelTypesLocator.GetType("Program").FirstOrDefault();
            var project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.AssemblyName.Equals(ProjectContext.AssemblyName, StringComparison.OrdinalIgnoreCase));
            var programDocument = project.GetUpdatedDocument(FileSystem, programType);
            //Modifying Program.cs document
            var docEditor = await DocumentEditor.CreateAsync(programDocument);
            var docRoot = docEditor.OriginalRoot as CompilationUnitSyntax;
            programCsFile.Usings = programCsFile.Usings.Union(new string[] { blazorIdentityModel.DbContextNamespace, blazorIdentityModel.BlazorIdentityNamespace }).ToArray();
            var docBuilder = new DocumentBuilder(docEditor, programCsFile, ConsoleLogger);
            var newRoot = docBuilder.AddUsings(new CodeChangeOptions());
            var useTopLevelStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(project.Documents.ToList());
            var builderIdentifier = ProjectModifierHelper.GetBuilderVariableIdentifierTransformation(newRoot.Members);
            //add code snippets/changes.
            if (programCsFile.Methods != null && programCsFile.Methods.Count > 0)
            {
                var globalMethod = programCsFile.Methods.Where(x => x.Key.Equals("Global", StringComparison.OrdinalIgnoreCase)).First().Value;
                var globalChanges = globalMethod.CodeChanges;
                if (builderIdentifier.HasValue)
                {
                    (string oldValue, string newValue) = builderIdentifier.Value;
                    globalChanges = ProjectModifierHelper.UpdateVariables(globalChanges, oldValue, newValue);
                }

                globalChanges = BlazorIdentityHelper.ApplyIdentityChanges(globalChanges, blazorIdentityModel.DbContextName, blazorIdentityModel.UserClassName, blazorIdentityModel.DatabaseProvider);
                if (useTopLevelStatements)
                {
                    newRoot = DocumentBuilder.ApplyChangesToMethod(newRoot, globalChanges) as CompilationUnitSyntax;
                }
                else
                {
                    var mainMethod = DocumentBuilder.GetMethodFromSyntaxRoot(newRoot, Main);
                    if (mainMethod != null)
                    {
                        var updatedMethod = DocumentBuilder.ApplyChangesToMethod(mainMethod.Body, globalChanges);
                        newRoot = newRoot?.ReplaceNode(mainMethod.Body, updatedMethod);
                    }
                }
            }

            //replace root node with all the updates.
            docEditor.ReplaceNode(docRoot, newRoot);
            //write to Program.cs file
            var changedDocument = docEditor.GetChangedDocument();
            var classFileTxt = await changedDocument.GetTextAsync();
            var classFileString = classFileTxt.ToString();
            classFileString = StringUtil.NormalizeLineEndings(classFileString);
            FileSystem.WriteAllText(programDocument.Name, classFileString);
            Logger.LogMessage($"Modified {programDocument.Name}.\n");
        }

        private void ExecuteTemplates(BlazorIdentityModel templateModel)
        {
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , templateModel }
            };

            foreach (var templateName in templateModel.FilesToGenerate)
            {
                ITextTransformation contextTemplate = GetBlazorIdentityTransformation(templateName);
                var templatedString = templateInvoker.InvokeTemplate(contextTemplate, dictParams);
                if (!string.IsNullOrEmpty(templatedString))
                {
                    // Files in Pages and Shared folders are Razor components, others are C# files
                    string extension = templateName.StartsWith("Pages", StringComparison.OrdinalIgnoreCase) ||
                                       templateName.StartsWith("Shared", StringComparison.OrdinalIgnoreCase) ? ".razor" : ".cs";
                    string templateNameWithNamespace = $"{templateModel.BlazorIdentityNamespace}.{templateName}";
                    string templatePath = StringUtil.ToPath(templateNameWithNamespace, templateModel.BaseOutputPath, ProjectContext.RootNamespace);
                    string templatedFilePath = $"{templatePath}{extension}";
                    var folderName = Path.GetDirectoryName(templatedFilePath);
                    if (!FileSystem.DirectoryExists(folderName))
                    {
                        FileSystem.CreateDirectory(folderName);
                    }

                    templatedString = StringUtil.NormalizeLineEndings(templatedString);
                    if (!FileSystem.FileExists(templatedFilePath))
                    {
                        FileSystem.WriteAllText(templatedFilePath, templatedString);
                        Logger.LogMessage($"Added Blazor identity file : {templatedFilePath}");
                    }
                }
            }
        }

        private void ExecuteApplicationUserTemplate(IdentityApplicationUserModel model)
        {
            var t4TemplatePath = GeneralT4Files.FirstOrDefault(x => x.Contains(nameof(IdentityApplicationUser)));
            var host = new TextTemplatingEngineHost { TemplateFile = t4TemplatePath };
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , model }
            };

            ITextTransformation t4TextTransformation = new IdentityApplicationUser
            {
                Session = host.CreateSession()
            };

            var templatedString = templateInvoker.InvokeTemplate(t4TextTransformation, dictParams);
            if (!string.IsNullOrEmpty(templatedString))
            {
                var identityDataPath = Path.Combine(AppInfo.ApplicationBasePath, "Data");
                if (!FileSystem.DirectoryExists(identityDataPath))
                {
                    FileSystem.CreateDirectory(identityDataPath);
                }

                string templatedFilePath = Path.Combine(identityDataPath, $"{model.UserClassName}.cs");
                templatedString = StringUtil.NormalizeLineEndings(templatedString);
                FileSystem.WriteAllText(templatedFilePath, templatedString);
                Logger.LogMessage($"Added IdentityUser class : {templatedFilePath}");
            }
        }

        private void ExecuteDbContextTemplate(IdentityDbContextModel model)
        {
            var t4TemplatePath = GeneralT4Files.FirstOrDefault(x => x.Contains(nameof(IdentityDbContext)));
            var host = new TextTemplatingEngineHost { TemplateFile = t4TemplatePath };
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , model }
            };

            ITextTransformation t4TextTransformation = new IdentityDbContext
            {
                Session = host.CreateSession()
            };

            var templatedString = templateInvoker.InvokeTemplate(t4TextTransformation, dictParams);
            if (!string.IsNullOrEmpty(templatedString))
            {
                var identityDataPath = Path.Combine(AppInfo.ApplicationBasePath, "Data");
                if (!FileSystem.DirectoryExists(identityDataPath))
                {
                    FileSystem.CreateDirectory(identityDataPath);
                }

                string templatedFilePath = Path.Combine(identityDataPath, $"{model.DbContextName}.cs");
                templatedString = StringUtil.NormalizeLineEndings(templatedString);
                FileSystem.WriteAllText(templatedFilePath, templatedString);
                Logger.LogMessage($"Added IdentityDbContext class : {templatedFilePath}");
            }
        }

        private void AddStaticFiles(BlazorIdentityModel templateModel)
        {
            var identityComponentsAccountPath = Path.Combine(templateModel.BaseOutputPath, "Components", "Account");
            if (!FileSystem.DirectoryExists(identityComponentsAccountPath))
            {
                FileSystem.CreateDirectory(identityComponentsAccountPath);
            }

            foreach (var (templateFolder, sourceFilePath) in AllBlazorIdentityStaticFiles)
            {
                var relativeFilePath = Path.GetRelativePath(templateFolder, sourceFilePath);
                var destinationFilePath = Path.Combine(identityComponentsAccountPath, relativeFilePath);

                var folderName = Path.GetDirectoryName(destinationFilePath);
                if (!FileSystem.DirectoryExists(folderName))
                {
                    FileSystem.CreateDirectory(folderName);
                }

                var fileContent = FileSystem.ReadAllText(sourceFilePath);
                FileSystem.WriteAllText(destinationFilePath, fileContent);
                Logger.LogMessage($"Added static file : {destinationFilePath}");
            }
        }

        private ITextTransformation GetBlazorIdentityTransformation(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return null;
            }

            var typeName = StringUtil.GetTypeNameFromNamespace(templateName);
            var t4TemplateType = BlazorIdentityTemplateTypes.FirstOrDefault(x => x.FullName.Contains(templateName) && x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            var t4TemplatePath = AllBlazorIdentityFiles[templateName];
            var host = new TextTemplatingEngineHost { TemplateFile = t4TemplatePath };
            try
            {
                ITextTransformation transformation = Activator.CreateInstance(t4TemplateType) as ITextTransformation;
                if (transformation != null)
                {
                    transformation.Session = host.CreateSession();
                }

                return transformation;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
