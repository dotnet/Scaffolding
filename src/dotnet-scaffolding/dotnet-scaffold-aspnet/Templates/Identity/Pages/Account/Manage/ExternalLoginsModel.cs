// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Identity.Pages.Account.Manage
{
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class ExternalLoginsModel : ExternalLoginsModelBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassNamespace));
            this.Write(";\r\n\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.IdentityNamespace));
            this.Write(".Pages.Account.Manage;\r\n\r\npublic class ExternalLoginsModel : PageModel\r\n{\r\n    pr" +
                    "ivate readonly UserManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _userManager;\r\n    private readonly SignInManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _signInManager;\r\n    private readonly IUserStore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _userStore;\r\n\r\n    public ExternalLoginsModel(\r\n        UserManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> userManager,\r\n        SignInManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> signInManager,\r\n        IUserStore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> userStore)\r\n    {\r\n        _userManager = userManager;\r\n        _signInManager " +
                    "= signInManager;\r\n        _userStore = userStore;\r\n    }\r\n\r\n    /// <summary>\r\n " +
                    "   ///     This API supports the ASP.NET Core Identity default UI infrastructure" +
                    " and is not intended to be used\r\n    ///     directly from your code. This API m" +
                    "ay change or be removed in future releases.\r\n    /// </summary>\r\n    public ILis" +
                    "t<UserLoginInfo>? CurrentLogins { get; set; }\r\n\r\n    /// <summary>\r\n    ///     " +
                    "This API supports the ASP.NET Core Identity default UI infrastructure and is not" +
                    " intended to be used\r\n    ///     directly from your code. This API may change o" +
                    "r be removed in future releases.\r\n    /// </summary>\r\n    public IList<Authentic" +
                    "ationScheme>? OtherLogins { get; set; }\r\n\r\n    /// <summary>\r\n    ///     This A" +
                    "PI supports the ASP.NET Core Identity default UI infrastructure and is not inten" +
                    "ded to be used\r\n    ///     directly from your code. This API may change or be r" +
                    "emoved in future releases.\r\n    /// </summary>\r\n    public bool ShowRemoveButton" +
                    " { get; set; }\r\n\r\n    /// <summary>\r\n    ///     This API supports the ASP.NET C" +
                    "ore Identity default UI infrastructure and is not intended to be used\r\n    ///  " +
                    "   directly from your code. This API may change or be removed in future releases" +
                    ".\r\n    /// </summary>\r\n    [TempData]\r\n    public string? StatusMessage { get; s" +
                    "et; }\r\n\r\n    public async Task<IActionResult> OnGetAsync()\r\n    {\r\n        var u" +
                    "ser = await _userManager.GetUserAsync(User);\r\n        if (user == null)\r\n       " +
                    " {\r\n            return NotFound($\"Unable to load user with ID \'{_userManager.Get" +
                    "UserId(User)}\'.\");\r\n        }\r\n\r\n        CurrentLogins = await _userManager.GetL" +
                    "oginsAsync(user);\r\n        OtherLogins = (await _signInManager.GetExternalAuthen" +
                    "ticationSchemesAsync())\r\n            .Where(auth => CurrentLogins.All(ul => auth" +
                    ".Name != ul.LoginProvider))\r\n            .ToList();\r\n\r\n        string? passwordH" +
                    "ash = null;\r\n        if (_userStore is IUserPasswordStore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> userPasswordStore)\r\n        {\r\n            passwordHash = await userPasswordSto" +
                    "re.GetPasswordHashAsync(user, HttpContext.RequestAborted);\r\n        }\r\n\r\n       " +
                    " ShowRemoveButton = passwordHash != null || CurrentLogins.Count > 1;\r\n        re" +
                    "turn Page();\r\n    }\r\n\r\n    public async Task<IActionResult> OnPostRemoveLoginAsy" +
                    "nc(string loginProvider, string providerKey)\r\n    {\r\n        var user = await _u" +
                    "serManager.GetUserAsync(User);\r\n        if (user == null)\r\n        {\r\n          " +
                    "  return NotFound($\"Unable to load user with ID \'{_userManager.GetUserId(User)}\'" +
                    ".\");\r\n        }\r\n\r\n        var result = await _userManager.RemoveLoginAsync(user" +
                    ", loginProvider, providerKey);\r\n        if (!result.Succeeded)\r\n        {\r\n     " +
                    "       StatusMessage = \"The external login was not removed.\";\r\n            retur" +
                    "n RedirectToPage();\r\n        }\r\n\r\n        await _signInManager.RefreshSignInAsyn" +
                    "c(user);\r\n        StatusMessage = \"The external login was removed.\";\r\n        re" +
                    "turn RedirectToPage();\r\n    }\r\n\r\n    public async Task<IActionResult> OnPostLink" +
                    "LoginAsync(string provider)\r\n    {\r\n        // Clear the existing external cooki" +
                    "e to ensure a clean login process\r\n        await HttpContext.SignOutAsync(Identi" +
                    "tyConstants.ExternalScheme);\r\n\r\n        // Request a redirect to the external lo" +
                    "gin provider to link a login for the current user\r\n        var redirectUrl = Url" +
                    ".Page(\"./ExternalLogins\", pageHandler: \"LinkLoginCallback\");\r\n        var proper" +
                    "ties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redire" +
                    "ctUrl, _userManager.GetUserId(User));\r\n        return new ChallengeResult(provid" +
                    "er, properties);\r\n    }\r\n\r\n    public async Task<IActionResult> OnGetLinkLoginCa" +
                    "llbackAsync()\r\n    {\r\n        var user = await _userManager.GetUserAsync(User);\r" +
                    "\n        if (user == null)\r\n        {\r\n            return NotFound($\"Unable to l" +
                    "oad user with ID \'{_userManager.GetUserId(User)}\'.\");\r\n        }\r\n\r\n        var " +
                    "userId = await _userManager.GetUserIdAsync(user);\r\n        var info = await _sig" +
                    "nInManager.GetExternalLoginInfoAsync(userId);\r\n        if (info == null)\r\n      " +
                    "  {\r\n            throw new InvalidOperationException($\"Unexpected error occurred" +
                    " loading external login info.\");\r\n        }\r\n\r\n        var result = await _userM" +
                    "anager.AddLoginAsync(user, info);\r\n        if (!result.Succeeded)\r\n        {\r\n  " +
                    "          StatusMessage = \"The external login was not added. External logins can" +
                    " only be associated with one account.\";\r\n            return RedirectToPage();\r\n " +
                    "       }\r\n\r\n        // Clear the existing external cookie to ensure a clean logi" +
                    "n process\r\n        await HttpContext.SignOutAsync(IdentityConstants.ExternalSche" +
                    "me);\r\n\r\n        StatusMessage = \"The external login was added.\";\r\n        return" +
                    " RedirectToPage();\r\n    }\r\n}\r\n");
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

private global::Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel _ModelField;

/// <summary>
/// Access the Model parameter of the template.
/// </summary>
private global::Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel Model
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
    this._ModelField = ((global::Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel)(this.Session["Model"]));
    ModelValueAcquired = true;
}
if ((ModelValueAcquired == false))
{
    string parameterValue = this.Host.ResolveParameterValue("Property", "PropertyDirectiveProcessor", "Model");
    if ((string.IsNullOrEmpty(parameterValue) == false))
    {
        global::System.ComponentModel.TypeConverter tc = global::System.ComponentModel.TypeDescriptor.GetConverter(typeof(global::Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel));
        if (((tc != null) 
                    && tc.CanConvertFrom(typeof(string))))
        {
            this._ModelField = ((global::Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel)(tc.ConvertFrom(parameterValue)));
            ModelValueAcquired = true;
        }
        else
        {
            this.Error("The type \'Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel\' of the par" +
                    "ameter \'Model\' did not match the type of the data passed to the template.");
        }
    }
}
if ((ModelValueAcquired == false))
{
    object data = global::Microsoft.DotNet.Scaffolding.TextTemplating.CallContext.LogicalGetData("Model");
    if ((data != null))
    {
        this._ModelField = ((global::Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel)(data));
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
    public class ExternalLoginsModelBase
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
