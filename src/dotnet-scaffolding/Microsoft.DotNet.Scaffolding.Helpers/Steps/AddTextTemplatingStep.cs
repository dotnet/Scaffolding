// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps;
/// <summary>
/// Provided all the required properties, this ScaffoldStep can template T4 onto a file on disk.
/// </summary>
internal class AddTextTemplatingStep : ScaffoldStep
{
    //Path to .tt (T4) template on disk (likely to be packed in a dotnet tool)
    public required string TemplatePath { get; init; }
    //the System.Type auto-generated object of the template (using TextTemplatingFilePreprocessor)
    public required Type TemplateType { get; init; }
    //output file path where the templated content will be written
    public required string OutputPath { get; init; }
    //the 'name' property of <#@ parameter #> in provided .tt template
    public required string TemplateModelName { get; init; }
    //the 'type' property of <#@ parameter #> in provided .tt template
    public required object TemplateModel { get; init; }
    public required IFileSystem FileSystem { get; init; }
    public required ILogger Logger { get; init; }
    public override Task<bool> ExecuteAsync()
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
        catch (Exception)
        {
            Logger.LogInformation($"Unable to create an instance of template type '{TemplateType.Name}'");
            return Task.FromResult(false);
        }

        if (textTransformation is not null)
        {
            var templatedString = templateInvoker.InvokeTemplate(textTransformation, dictParams);
            var outputFolderPath = Path.GetDirectoryName(OutputPath);
            //create the directory for the output file incase not already there.
            if (!string.IsNullOrEmpty(templatedString) && !string.IsNullOrEmpty(outputFolderPath))
            {
                if (!FileSystem.DirectoryExists(outputFolderPath))
                {
                    FileSystem.CreateDirectory(outputFolderPath);
                }

                FileSystem.WriteAllText(OutputPath, templatedString);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}
