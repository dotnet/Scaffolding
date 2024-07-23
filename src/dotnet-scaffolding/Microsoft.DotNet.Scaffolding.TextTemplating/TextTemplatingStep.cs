// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.TextTemplating;
/// <summary>
/// Provided all the required properties, this ScaffoldStep can template T4 onto a file on disk.
/// </summary>
public class TextTemplatingStep : ScaffoldStep
{
    //Path to .tt (T4) template on disk (likely to be packed in a dotnet tool)
    public required string TemplatePath { get; set; }
    //the System.Type auto-generated object of the template (using TextTemplatingFilePreprocessor)
    public required Type TemplateType { get; set; }
    //output file path where the templated content will be written (should include the extension if one is wanted)
    public required string OutputPath { get; set; }
    //the 'name' property of <#@ parameter #> in provided .tt template
    public required string TemplateModelName { get; set; }
    //the 'type' property of <#@ parameter #> in provided .tt template
    public required object TemplateModel { get; set; }
    public override Task ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var templateInvoker = new TemplateInvoker();
        var dictParams = new Dictionary<string, object>()
        {
            { TemplateModelName, TemplateModel }
        };

        var host = new TextTemplatingEngineHost { TemplateFile = TemplatePath };
        ITextTransformation? textTransformation;
        try
        {
            //need to re-instantiate the ITextTransformation type provided (using the TextTemplatingFilePreprocessor in the scaffolder)
            textTransformation = Activator.CreateInstance(TemplateType) as ITextTransformation;
            if (textTransformation != null)
            {
                textTransformation.Session = host.CreateSession();
            }
        }
        catch (Exception ex)
        {
            var newException = new Exception($"Unable to create an instance of template type '{TemplateType.Name}'", ex);
            throw newException;
        }

        if (textTransformation is not null)
        {
            var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
            var outputFolderPath = Path.GetDirectoryName(OutputPath);
            //create the directory for the output file incase not already there.
            if (!string.IsNullOrEmpty(templatedString) && !string.IsNullOrEmpty(outputFolderPath))
            {
                if (!Directory.Exists(outputFolderPath))
                {
                    Directory.CreateDirectory(outputFolderPath);
                }

                File.WriteAllText(OutputPath, templatedString);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}
