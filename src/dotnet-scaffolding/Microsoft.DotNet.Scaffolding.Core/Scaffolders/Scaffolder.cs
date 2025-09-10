// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

/// <summary>
/// Default implementation of the <see cref="IScaffolder"/> interface, representing a scaffolder with options and steps.
/// </summary>
public class Scaffolder : IScaffolder
{
    private readonly string _name;
    private readonly string _displayName;
    private readonly List<string> _categories;
    private readonly string? _description;
    private readonly List<ScaffolderOption> _options;
    private readonly List<ScaffoldStep> _steps;
    private readonly List<ScaffoldStepPreparer> _preparers;

    /// <inheritdoc/>
    public string Name => _name;
    /// <inheritdoc/>
    public string DisplayName => _displayName;
    /// <inheritdoc/>
    public IEnumerable<string> Categories => _categories;
    /// <inheritdoc/>
    public string? Description => _description;
    /// <inheritdoc/>
    public IEnumerable<ScaffolderOption> Options => _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scaffolder"/> class.
    /// </summary>
    /// <param name="name">The name of the scaffolder.</param>
    /// <param name="displayName">The display name of the scaffolder.</param>
    /// <param name="categories">The categories for the scaffolder.</param>
    /// <param name="description">The description of the scaffolder.</param>
    /// <param name="options">The options for the scaffolder.</param>
    /// <param name="steps">The steps for the scaffolder.</param>
    /// <param name="preparers">The preparers for the scaffolder steps.</param>
    internal Scaffolder(string name, string displayName, List<string> categories, string? description, List<ScaffolderOption> options, List<ScaffoldStep> steps, List<ScaffoldStepPreparer> preparers)
    {
        _name = name;
        _displayName = displayName;
        _categories = categories;
        _description = description;
        _options = options;
        _steps = steps;
        _preparers = preparers;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScaffolderContext context)
    {
        for (int stepIndex = 0; stepIndex < _steps.Count; stepIndex++)
        {
            var step = _steps[stepIndex];
            var preparer = _preparers[stepIndex];
            preparer.RunPreExecute(step, context);
            if (!step.SkipStep)
            {
                var stepResult = await step.ExecuteAsync(context);
                if (!stepResult && !step.ContinueOnError)
                {
                    break;
                }
            }
            preparer.RunPostExecute(step, context);
        }
    }
}
