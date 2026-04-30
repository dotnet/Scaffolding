// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
/// Provided all the required properties, this ScaffoldStep can template T4 onto a file on disk.
/// </summary>
public class TextTemplatingStep : ScaffoldStep
{
    /// <summary>
    /// Display name for the step (used in logs/UI).
    /// </summary>
    public string? DisplayName { get; set; } = "files";

    /// <summary>
    /// The collection of T4 text templating properties to process.
    /// </summary>
    public required IEnumerable<TextTemplatingProperty> TextTemplatingProperties { get; set; }

    /// <summary>
    /// By default Overwrite should be false. If true, overwrite existing files. Otherwise, do not overwrite.
    /// </summary>
    public bool Overwrite { get; set; }

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextTemplatingStep"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public TextTemplatingStep(ILogger<TextTemplatingStep> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes the text templating step, generating files as specified.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (TextTemplatingProperties is null || !TextTemplatingProperties.Any())
        {
            _logger.LogError("Invalid/empty value provided for the 'TextTemplatingStep.TextTemplatingProperties' variable");
            return Task.FromResult(false);
        }

        var templateInvoker = new TemplateInvoker();
        _logger.LogInformation($"Adding {DisplayName}...");
        foreach (var templatingProperty in TextTemplatingProperties)
        {
            var dictParams = new Dictionary<string, object>()
            {
                { templatingProperty.TemplateModelName, templatingProperty.TemplateModel }
            };

            var host = new TextTemplatingEngineHost { TemplateFile = templatingProperty.TemplatePath };
            ITextTransformation? textTransformation = null;
            try
            {
                // Re-instantiate the ITextTransformation type provided (using the TextTemplatingFilePreprocessor in the scaffolder)
                textTransformation = Activator.CreateInstance(templatingProperty.TemplateType) as ITextTransformation;
                if (textTransformation != null)
                {
                    textTransformation.Session = host.CreateSession();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to create an instance of template type '{templatingProperty.TemplateType.Name}'");
                _logger.LogError(ex.Message);
            }

            if (textTransformation is not null)
            {
                var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
                var outputFolderPath = Path.GetDirectoryName(templatingProperty.OutputPath);
                // Create the directory for the output file in case not already there.
                if (!string.IsNullOrEmpty(templatedString) && !string.IsNullOrEmpty(outputFolderPath))
                {
                    if (!Directory.Exists(outputFolderPath))
                    {
                        Directory.CreateDirectory(outputFolderPath);
                    }

                    // If Overwrite is true, write file, or if it doesn't exist
                    if (Overwrite || !File.Exists(templatingProperty.OutputPath))
                    {
                        File.WriteAllText(templatingProperty.OutputPath, templatedString, new UTF8Encoding(false));
                    }
                }
            }
        }

        _logger.LogInformation("Done\n");
        return Task.FromResult(true);
    }
}
