// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.TextTemplating;
/// <summary>
/// Provided all the required properties, this ScaffoldStep can template T4 onto a file on disk.
/// </summary>
public class TextTemplatingStep : ScaffoldStep
{
    public string? DisplayName { get; set; } = "files";
    public required IEnumerable<TextTemplatingProperty> TextTemplatingProperties { get; set; }
    //by default, Overwrite should be false, if a file already exists, don't overwrite it.
    public bool Overwrite { get; set; }

    private readonly ILogger _logger;

    public TextTemplatingStep(ILogger<TextTemplatingStep> logger)
    {
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (TextTemplatingProperties is null || !TextTemplatingProperties.Any())
        {
            _logger.LogError("Invalid/empty value provided for the 'TextTemplatingStep.TextTemplatingProperties' variable");
            return Task.FromResult(false);
        }

        var templateInvoker = new TemplateInvoker();
        _logger.LogInformation($"Adding {DisplayName}...");
        foreach(var templatingProperty in TextTemplatingProperties)
        {
            var dictParams = new Dictionary<string, object>()
            {
                { templatingProperty.TemplateModelName, templatingProperty.TemplateModel }
            };

            var host = new TextTemplatingEngineHost { TemplateFile = templatingProperty.TemplatePath };
            ITextTransformation? textTransformation = null;
            try
            {
                //need to re-instantiate the ITextTransformation type provided (using the TextTemplatingFilePreprocessor in the scaffolder)
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
                //create the directory for the output file incase not already there.
                if (!string.IsNullOrEmpty(templatedString) && !string.IsNullOrEmpty(outputFolderPath))
                {
                    if (!Directory.Exists(outputFolderPath))
                    {
                        Directory.CreateDirectory(outputFolderPath);
                    }

                    //if Overwrite is true, write file, or if it doesn't exist
                    if (Overwrite || !File.Exists(templatingProperty.OutputPath))
                    {
                        File.WriteAllText(templatingProperty.OutputPath, templatedString);
                    }
                }
            }
        }

        _logger.LogInformation("Done\n");
        return Task.FromResult(true);
    }
}
