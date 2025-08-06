// Auto-generated placeholder for NotFound.tt template
namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor
{
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System;
    
    public partial class NotFound : NotFoundBase
    {
        public virtual string TransformText()
        {
            this.Write("@page \"/not-found\"\r\n@layout MainLayout\r\n\r\n<PageTitle>Not Found</PageTitle>\r\n\r\n<h3>Not Found</h3>\r\n<p>Sorry, the content you are looking for does not exist.</p>\r\n\r\n<a href=\"/\" class=\"btn btn-primary\">Return to Home</a>");
            return this.GenerationEnvironment.ToString();
        }
        
        private global::Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost hostValue;
        public virtual global::Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost Host
        {
            get { return this.hostValue; }
            set { this.hostValue = value; }
        }

        private global::Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor.BlazorModel _ModelField;
        private global::Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor.BlazorModel Model
        {
            get { return this._ModelField; }
        }

        public virtual void Initialize()
        {
            if (this.Errors.HasErrors == false)
            {
                bool ModelValueAcquired = false;
                if (this.Session != null && this.Session.ContainsKey("Model"))
                {
                    this._ModelField = ((global::Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor.BlazorModel)(this.Session["Model"]));
                    ModelValueAcquired = true;
                }
                if ((ModelValueAcquired == false))
                {
                    string parameterValue = this.Host.ResolveParameterValue("Property", "PropertyDirectiveProcessor", "Model");
                    if ((string.IsNullOrEmpty(parameterValue) == false))
                    {
                        global::System.ComponentModel.TypeConverter tc = global::System.ComponentModel.TypeDescriptor.GetConverter(typeof(global::Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor.BlazorModel));
                        if (((tc != null) && tc.CanConvertFrom(typeof(string))))
                        {
                            this._ModelField = ((global::Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor.BlazorModel)(tc.ConvertFrom(parameterValue)));
                            ModelValueAcquired = true;
                        }
                        else
                        {
                            this.Error("The type 'Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor.BlazorModel' of the parameter 'Model' did not match the type of the data passed to the template.");
                        }
                    }
                }
                if ((ModelValueAcquired == false))
                {
                    object data = global::Microsoft.DotNet.Scaffolding.Shared.T4Templating.CallContext.LogicalGetData("Model");
                    if ((data != null))
                    {
                        this._ModelField = ((global::Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor.BlazorModel)(data));
                    }
                }
            }
        }
    }
    
    #region Base class
    public class NotFoundBase
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
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get { return this.sessionField; }
            set { this.sessionField = value; }
        }
        #endregion
        
        #region Transform-time helpers
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            if (((this.GenerationEnvironment.Length == 0) || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        #endregion
    }
    #endregion
}