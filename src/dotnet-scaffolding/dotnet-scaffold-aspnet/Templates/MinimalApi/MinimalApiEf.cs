// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.MinimalApi
{
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class MinimalApiEf : MinimalApiEfBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {

    string modelName = Model.ModelInfo.ModelTypeName;
    string dbProvider = Model.DbContextInfo.DatabaseProvider;
    string routePrefix = "/api/" + modelName;
    string endPointsClassName = Model.EndpointsClassName;
    string methodName = $"Map{modelName}Endpoints";
    string pluralModel = Model.ModelInfo.ModelTypePluralName;
    string getAllModels = $"GetAll{pluralModel}";
    string getModelById = $"Get{modelName}ById";
    string deleteModel = $"Delete{modelName}";
    string createModel = $"Create{modelName}";
    string updateModel = $"Update{modelName}";
    string dbContextName = Model.DbContextInfo.DbContextClassName;
    var entitySetName = Model.DbContextInfo.EntitySetVariableName ?? modelName;
    var entitySetNoTracking = $"{entitySetName}.AsNoTracking()";
    var entityProperties = Model.ModelInfo.ModelProperties;
    var primaryKeyName = Model.ModelInfo.PrimaryKeyName;
    var primaryKeyNameLowerCase = primaryKeyName.ToLowerInvariant();
    var primaryKeyShortTypeName = Model.ModelInfo.PrimaryKeyShortTypeName;
    var primaryKeyType = Model.ModelInfo.PrimaryKeyTypeName;
    var modelList = $"List<{modelName}>";
    var modelToList = $"{entitySetName}.ToListAsync()";
    var findModel = $"{entitySetName}.FindAsync({primaryKeyNameLowerCase})";
    var add = $"{entitySetName}.Add({Model.ModelInfo.ModelVariable})";
    var remove = $"{entitySetName}.Remove({Model.ModelInfo.ModelVariable})";
    string resultsExtension = Model.UseTypedResults ? "TypedResults" : "Results";
    string typedTaskWithNotFound = Model.UseTypedResults ? $"Task<Results<Ok<{modelName}>, NotFound>>" : "";
    string typedTaskOkNotFound = Model.UseTypedResults ? $"Task<Results<Ok, NotFound>>" : "";
    string typedTaskWithNoContent = Model.UseTypedResults ? $"Task<Results<NotFound, NoContent>>" : "";
    string resultsNotFound = $"{resultsExtension}.NotFound()";
    string resultsOkModel = $"{resultsExtension}.Ok(model)";
    string resultsOkEmpty = $"{resultsExtension}.Ok()";
    string resultsNoContent = $"{resultsExtension}.NoContent()";
    string resultsOkModelVariable = $"{resultsExtension}.Ok({Model.ModelInfo.ModelVariable})";
    string createdApiVar = string.Format("$\"{0}/{{{1}.{2}}}\",{3}", routePrefix, Model.ModelInfo.ModelVariable, primaryKeyName, Model.ModelInfo.ModelVariable);
    string resultsCreated = $"{resultsExtension}.Created(" + $"{createdApiVar}" + ")";
    string builderExtensionSpaces = new string(' ', 8);
        string group = Model.OpenAPI
        ? $"var group = routes.MapGroup(\"{routePrefix}\").WithTags(nameof({Model.ModelInfo.ModelTypeName}));"
        : $"var group = routes.MapGroup(\"{routePrefix}\");";

            this.Write("using Microsoft.AspNetCore.Http.HttpResults;\r\nusing Microsoft.EntityFrameworkCore" +
                    ";\r\n");
 if (!string.IsNullOrEmpty(Model.DbContextInfo.DbContextNamespace))
{
            this.Write("using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.DbContextInfo.DbContextNamespace));
            this.Write(";\r\n");

}
            this.Write("\r\npublic static class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.EndpointsClassName));
            this.Write("\r\n{\r\n    public static void ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.EndpointsMethodName));
            this.Write("(this IEndpointRouteBuilder routes)\r\n    {\r\n        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(group));
            this.Write("\r\n\r\n        group.MapGet(\"/\", async (");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbContextName));
            this.Write(" db) =>\r\n        {\r\n            return await db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(modelToList));
            this.Write(";\r\n        })");

        string builderExtensions = $".WithName(\"{getAllModels}\")";
        if(Model.OpenAPI)
        {
            builderExtensions += $"\r\n    .WithOpenApi()";
        }
        if(!Model.UseTypedResults)
        {
            builderExtensions += $"\r\n    .Produces<{modelList}>(StatusCodes.Status200OK)";
        }
        
            this.Write("\r\n        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(builderExtensions));
            this.Write(";\r\n\r\n        group.MapGet(\"/{id}\", async ");
            this.Write(this.ToStringHelper.ToStringWithCulture(typedTaskWithNotFound));
            this.Write(" (");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyShortTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbContextName));
            this.Write(" db) =>\r\n        {\r\n            return await db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(entitySetNoTracking));
            this.Write("\r\n                .FirstOrDefaultAsync(model => model.");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyName));
            this.Write(" == ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(")\r\n                is ");
            this.Write(this.ToStringHelper.ToStringWithCulture(modelName));
            this.Write(" model\r\n                    ? ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsOkModel));
            this.Write("\r\n                    : ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsNotFound));
            this.Write(";\r\n        })");

        builderExtensions = $".WithName(\"{getModelById}\")";
        if(Model.OpenAPI)
        {
            builderExtensions += $"\r\n    .WithOpenApi()";
        }
        if(!Model.UseTypedResults)
        {
            builderExtensions += $"\r\n    .Produces<{Model.ModelInfo.ModelTypeName}>(StatusCodes.Status200OK)";
            builderExtensions += $"\r\n    .Produces(StatusCodes.Status404NotFound)";
        }
        
            this.Write("\r\n        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(builderExtensions));
            this.Write(";\r\n\r\n    ");

        if (dbProvider == "cosmos-efcore")
        {

            this.Write("        group.MapPut(\"/{id}\", async ");
            this.Write(this.ToStringHelper.ToStringWithCulture(typedTaskWithNoContent));
            this.Write(" (");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyShortTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(modelName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ModelInfo.ModelVariable));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbContextName));
            this.Write(" db) =>\r\n        {\r\n            var foundModel = await db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(findModel));
            this.Write(";\r\n\r\n            if (foundModel is null)\r\n            {\r\n                return ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsNotFound));
            this.Write(";\r\n            }\r\n\r\n            db.Update(");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ModelInfo.ModelVariable));
            this.Write(");\r\n            await db.SaveChangesAsync();\r\n\r\n            return ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsNoContent));
            this.Write(";\r\n        })\r\n    ");

        }

        if (dbProvider != "cosmos-efcore")
        {

            this.Write("    group.MapPut(\"/{id}\", async ");
            this.Write(this.ToStringHelper.ToStringWithCulture(typedTaskOkNotFound));
            this.Write(" (");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyShortTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(modelName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ModelInfo.ModelVariable));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbContextName));
            this.Write(" db) =>\r\n        {\r\n            var affected = await db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(entitySetName));
            this.Write("\r\n                .Where(model => model.");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyName));
            this.Write(" == ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(")\r\n                .ExecuteUpdateAsync(setters => setters\r\n        ");

            //should be atleast one property (primary key)
            foreach(var modelProperty in entityProperties)
            {
                string modelPropertyName = modelProperty.Name;
                string setPropertyString = $".SetProperty(m => m.{modelPropertyName}, {Model.ModelInfo.ModelVariable}.{modelPropertyName})";
        
            this.Write("        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(setPropertyString));
            this.Write("\r\n        ");

            }
        
            this.Write(");\r\n\r\n            return affected == 1 ? ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsOkEmpty));
            this.Write(" : ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsNotFound));
            this.Write(";\r\n        })\r\n    ");

        }

        builderExtensions = $".WithName(\"{updateModel}\")";
        if (Model.OpenAPI)
        {
            builderExtensions += $"\r\n{builderExtensionSpaces}.WithOpenApi()";
        }
        if (!Model.UseTypedResults)
        {
            builderExtensions += $"\r\n{builderExtensionSpaces}.Produces(StatusCodes.Status404NotFound)";
            builderExtensions += $"\r\n{builderExtensionSpaces}.Produces(StatusCodes.Status204NoContent)";
        }

    
            this.Write("    ");
            this.Write(this.ToStringHelper.ToStringWithCulture(builderExtensions));
            this.Write(";\r\n\r\n        group.MapPost(\"/\", async (");
            this.Write(this.ToStringHelper.ToStringWithCulture(modelName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ModelInfo.ModelVariable));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbContextName));
            this.Write(" db) =>\r\n        {\r\n            db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(add));
            this.Write(";\r\n            await db.SaveChangesAsync();\r\n            return ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsCreated));
            this.Write(";\r\n        })\r\n        ");

        builderExtensions = $".WithName(\"{createModel}\")";
        if(Model.OpenAPI)
        {
            builderExtensions+= $"\r\n    .WithOpenApi()";
        }
        if (!Model.UseTypedResults)
        {
            builderExtensions += $"\r\n    .Produces<{Model.ModelInfo.ModelTypeName}>(StatusCodes.Status201Created)";
        }
    
            this.Write(this.ToStringHelper.ToStringWithCulture(builderExtensions));
            this.Write(";\r\n\r\n        ");

        if (dbProvider == "CosmosDb")
        {

            this.Write("group.MapDelete(\"/{id}\", async ");
            this.Write(this.ToStringHelper.ToStringWithCulture(typedTaskWithNotFound));
            this.Write(" (");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyShortTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbContextName));
            this.Write(" db) =>\r\n        {\r\n            if (await db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(findModel));
            this.Write(" is ");
            this.Write(this.ToStringHelper.ToStringWithCulture(modelName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.ModelInfo.ModelVariable));
            this.Write(")\r\n            {\r\n                db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(remove));
            this.Write(";\r\n                await db.SaveChangesAsync();\r\n                return ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsOkModelVariable));
            this.Write(";\r\n            }\r\n\r\n            return ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsNotFound));
            this.Write(";\r\n        })\r\n        ");

        }

        if (dbProvider != "CosmosDb")
        {

            this.Write("group.MapDelete(\"/{id}\", async ");
            this.Write(this.ToStringHelper.ToStringWithCulture(typedTaskOkNotFound));
            this.Write(" (");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyShortTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbContextName));
            this.Write(" db) =>\r\n        {\r\n            var affected = await db.");
            this.Write(this.ToStringHelper.ToStringWithCulture(entitySetName));
            this.Write("\r\n                .Where(model => model.");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyName));
            this.Write(" == ");
            this.Write(this.ToStringHelper.ToStringWithCulture(primaryKeyNameLowerCase));
            this.Write(")\r\n                .ExecuteDeleteAsync();\r\n\r\n            return affected == 1 ? ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsOkEmpty));
            this.Write(" : ");
            this.Write(this.ToStringHelper.ToStringWithCulture(resultsNotFound));
            this.Write(";\r\n        })\r\n        ");

        }

        builderExtensions = $".WithName(\"{deleteModel}\")";
        if (Model.OpenAPI)
        {
            builderExtensions += $"\r\n{builderExtensionSpaces}.WithOpenApi()";
        }
        if (!Model.UseTypedResults)
        {
            builderExtensions += $"\r\n{builderExtensionSpaces}.Produces<{modelName}>(StatusCodes.Status200OK)";
            builderExtensions += $"\r\n{builderExtensionSpaces}.Produces(StatusCodes.Status404NotFound)";
        }
    
            this.Write(this.ToStringHelper.ToStringWithCulture(builderExtensions));
            this.Write(";\r\n    }\r\n}\r\n");
            return this.GenerationEnvironment.ToString();
        }
        private global::Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost hostValue;
        /// <summary>
        /// The current host for the text templating engine
        /// </summary>
        public virtual global::Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost Host
        {
            get
            {
                return this.hostValue;
            }
            set
            {
                this.hostValue = value;
            }
        }

private global::Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi.MinimalApiModel _ModelField;

/// <summary>
/// Access the Model parameter of the template.
/// </summary>
private global::Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi.MinimalApiModel Model
{
    get
    {
        return this._ModelField;
    }
}


/// <summary>
/// Initialize the template
/// </summary>
public virtual void Initialize()
{
    if ((this.Errors.HasErrors == false))
    {
bool ModelValueAcquired = false;
if (this.Session.ContainsKey("Model"))
{
    this._ModelField = ((global::Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi.MinimalApiModel)(this.Session["Model"]));
    ModelValueAcquired = true;
}
if ((ModelValueAcquired == false))
{
    string parameterValue = this.Host.ResolveParameterValue("Property", "PropertyDirectiveProcessor", "Model");
    if ((string.IsNullOrEmpty(parameterValue) == false))
    {
        global::System.ComponentModel.TypeConverter tc = global::System.ComponentModel.TypeDescriptor.GetConverter(typeof(global::Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi.MinimalApiModel));
        if (((tc != null) 
                    && tc.CanConvertFrom(typeof(string))))
        {
            this._ModelField = ((global::Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi.MinimalApiModel)(tc.ConvertFrom(parameterValue)));
            ModelValueAcquired = true;
        }
        else
        {
            this.Error("The type \'Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi.MinimalApiMo" +
                    "del\' of the parameter \'Model\' did not match the type of the data passed to the t" +
                    "emplate.");
        }
    }
}
if ((ModelValueAcquired == false))
{
    object data = global::Microsoft.DotNet.Scaffolding.Helpers.T4Templating.CallContext.LogicalGetData("Model");
    if ((data != null))
    {
        this._ModelField = ((global::Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi.MinimalApiModel)(data));
    }
}


    }
}


    }
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class MinimalApiEfBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        public System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
