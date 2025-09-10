// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Builder for configuring and creating a scaffolder with options and steps.
/// </summary>
internal class ScaffoldBuilder(string name) : IScaffoldBuilder
{
    // Default category for scaffolders
    private const string DEFAULT_CATEGORY = ScaffolderConstants.DEFAULT_CATEGORY;

    // List of options for the scaffolder
    private readonly List<ScaffolderOption> _options = [];
    // List of step preparers for the scaffolder
    private readonly List<ScaffoldStepPreparer> _stepPreparers = [];
    // Name of the scaffolder
    private readonly string _name = FixName(name);
    // Display name of the scaffolder
    private string? _displayName;
    // Categories for the scaffolder
    private HashSet<string> _categories = [DEFAULT_CATEGORY];
    // Description of the scaffolder
    private string? _description;

    // Gets the name of the scaffolder
    internal string Name => _name;
    // Gets the display name of the scaffolder
    internal string DisplayName => _displayName ?? System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(name);
    // Gets the categories of the scaffolder
    internal HashSet<string> Categories => _categories;
    // Gets the description of the scaffolder
    internal string? Description => _description;
    // Gets the options for the scaffolder
    internal IEnumerable<ScaffolderOption> Options => _options;
    // Gets the step preparers for the scaffolder
    internal IEnumerable<ScaffoldStepPreparer> StepPreparers => _stepPreparers;

    /// <inheritdoc/>
    public IScaffoldBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    /// <inheritdoc/>
    public IScaffoldBuilder WithCategory(string category)
    {
        _categories.Add(category);
        return this;
    }

    /// <inheritdoc/>
    public IScaffoldBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <inheritdoc/>
    public IScaffoldBuilder WithOption(ScaffolderOption option)
    {
        _options.Add(option);
        return this;
    }

    /// <inheritdoc/>
    public IScaffoldBuilder WithOptions(IEnumerable<ScaffolderOption> options)
    {
        _options.AddRange(options);
        return this;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public IScaffolder Build(IServiceProvider serviceProvider)
    {
        List<ScaffoldStep> steps = [];
        foreach (var step in _stepPreparers)
        {
            var stepType = step.GetStepType();
            // Resolve step instance from service provider
            var stepInstance = serviceProvider.GetService(stepType)
                ?? throw new InvalidOperationException($"Could not find '{stepType.Name}' ScaffoldStep.");

            steps.Add((ScaffoldStep)stepInstance);    
        }

        return new Scaffolder(Name, DisplayName, Categories.ToList(), Description, _options, steps, _stepPreparers);
    }

    /// <summary>
    /// Normalizes the scaffolder name by replacing spaces and converting to lower case.
    /// </summary>
    /// <param name="name">The original name.</param>
    /// <returns>The normalized name.</returns>
    private static string FixName(string name)
        => name.Replace(" ", "-").ToLowerInvariant();
}
