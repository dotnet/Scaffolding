// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

public class Scaffolder : IScaffolder
{
    private readonly string _name;
    private readonly string _displayName;
    private readonly string _category;
    private readonly string? _description;
    private readonly List<ScaffolderOption> _options;
    private readonly List<ScaffoldStep> _steps;
    private readonly List<ScaffoldStepPreparer> _preparers;
    public string Name => _name;
    public string DisplayName => _displayName;
    public string Category => _category;
    public string? Description => _description;
    public IEnumerable<ScaffolderOption> Options => _options;

    internal Scaffolder(string name, string displayName, string category, string? description, List<ScaffolderOption> options, List<ScaffoldStep> steps, List<ScaffoldStepPreparer> preparers)
    {
        _name = name;
        _displayName = displayName;
        _category = category;
        _description = description;
        _options = options;
        _steps = steps;
        _preparers = preparers;
    }

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
