namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
/// Services for invoking T4 text templates for code generation.
/// </summary>
internal interface ITemplateInvoker
{
    /// <summary>
    /// Invokes a T4 text template and returns the result.
    /// </summary>
    /// <param name="template">ITextTransformation template object.</param>
    /// <param name="templateParameters">Parameters for template execution.
    /// These parameters can be accessed in text template using a parameter directive.
    /// The values passed in must be either serializable or 
    /// extend <see cref="System.MarshalByRefObject"/> type.</param>
    /// <returns>Generated code if there were no processing errors. Throws 
    /// <see cref="System.InvalidOperationException" /> otherwise.
    /// </returns>
    string InvokeTemplate(
        ITextTransformation template,
        IDictionary<string, object> templateParameters);
}
