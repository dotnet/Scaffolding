// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal class ScaffoldBuilder(string name) : IScaffoldBuilder
{
    private const string DEFAULT_CATEGORY = ScaffolderConstants.DEFAULT_CATEGORY;

    private readonly List<ScaffolderOption> _options = [];
    private readonly List<ScaffoldStepPreparer> _stepPreparers = [];
    private readonly string _name = FixName(name);
    private string? _displayName;
    private HashSet<string> _categories = [DEFAULT_CATEGORY];
    private string? _description;

    internal string Name => _name;
    internal string DisplayName => _displayName ?? System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(name);
    internal HashSet<string> Categories => _categories;
    internal string? Description => _description;
    internal IEnumerable<ScaffolderOption> Options => _options;
    internal IEnumerable<ScaffoldStepPreparer> StepPreparers => _stepPreparers;

    public IScaffoldBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public IScaffoldBuilder WithCategory(string category)
    {
        _categories.Add(category);
        return this;
    }

    public IScaffoldBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public IScaffoldBuilder WithOption(ScaffolderOption option)
    {
        _options.Add(option);
        return this;
    }

    public IScaffoldBuilder WithOptions(IEnumerable<ScaffolderOption> options)
    {
        _options.AddRange(options);
        return this;
    }

    public IScaffoldBuilder WithStep<TStep>(Action<ScaffoldStepConfigurator<TStep>>? preExecute = null, Action<ScaffoldStepConfigurator<TStep>>? postExecute = null) where TStep : ScaffoldStep
    {
        var preparer = new ScaffoldStepPreparer<TStep>()
        {
            PreExecute = preExecute,
            PostExecute = postExecute
        };

        _stepPreparers.Add(preparer);
        return this;
    }

    public IScaffolder Build(IServiceProvider serviceProvider)
    {
        List<ScaffoldStep> steps = [];
        foreach (var step in _stepPreparers)
        {
            var stepType = step.GetStepType();
            var stepInstance = serviceProvider.GetService(stepType)
                ?? throw new InvalidOperationException($"Could not find '{stepType.Name}' ScaffoldStep.");

            steps.Add((ScaffoldStep)stepInstance);    
        }

        return new Scaffolder(Name, DisplayName, Categories.ToList(), Description, _options, steps, _stepPreparers);
    }

    private static string FixName(string name)
        => name.Replace(" ", "-").ToLowerInvariant();
}
