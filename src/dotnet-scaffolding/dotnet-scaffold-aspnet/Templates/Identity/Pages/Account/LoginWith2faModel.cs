// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Identity.Pages.Account
{
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class LoginWith2faModel : LoginWith2faModelBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassNamespace));
            this.Write(";\r\n\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.IdentityNamespace));
            this.Write(".Pages.Account;\r\n\r\npublic class LoginWith2faModel : PageModel\r\n{\r\n    private rea" +
                    "donly SignInManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _signInManager;\r\n    private readonly UserManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _userManager;\r\n    private readonly ILogger<LoginWith2faModel> _logger;\r\n\r\n    " +
                    "public LoginWith2faModel(\r\n        SignInManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> signInManager,\r\n        UserManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> userManager,\r\n        ILogger<LoginWith2faModel> logger)\r\n    {\r\n        _signI" +
                    "nManager = signInManager;\r\n        _userManager = userManager;\r\n        _logger " +
                    "= logger;\r\n    }\r\n\r\n    /// <summary>\r\n    ///     This API supports the ASP.NET" +
                    " Core Identity default UI infrastructure and is not intended to be used\r\n    ///" +
                    "     directly from your code. This API may change or be removed in future releas" +
                    "es.\r\n    /// </summary>\r\n    [BindProperty]\r\n    public InputModel Input { get; " +
                    "set; } = default!;\r\n\r\n    /// <summary>\r\n    ///     This API supports the ASP.N" +
                    "ET Core Identity default UI infrastructure and is not intended to be used\r\n    /" +
                    "//     directly from your code. This API may change or be removed in future rele" +
                    "ases.\r\n    /// </summary>\r\n    public bool RememberMe { get; set; }\r\n\r\n    /// <" +
                    "summary>\r\n    ///     This API supports the ASP.NET Core Identity default UI inf" +
                    "rastructure and is not intended to be used\r\n    ///     directly from your code." +
                    " This API may change or be removed in future releases.\r\n    /// </summary>\r\n    " +
                    "public string? ReturnUrl { get; set; }\r\n\r\n    /// <summary>\r\n    ///     This AP" +
                    "I supports the ASP.NET Core Identity default UI infrastructure and is not intend" +
                    "ed to be used\r\n    ///     directly from your code. This API may change or be re" +
                    "moved in future releases.\r\n    /// </summary>\r\n    public class InputModel\r\n    " +
                    "{\r\n        /// <summary>\r\n        ///     This API supports the ASP.NET Core Ide" +
                    "ntity default UI infrastructure and is not intended to be used\r\n        ///     " +
                    "directly from your code. This API may change or be removed in future releases.\r\n" +
                    "        /// </summary>\r\n        [Required]\r\n        [StringLength(7, ErrorMessag" +
                    "e = \"The {0} must be at least {2} and at max {1} characters long.\", MinimumLengt" +
                    "h = 6)]\r\n        [DataType(DataType.Text)]\r\n        [Display(Name = \"Authenticat" +
                    "or code\")]\r\n        public string TwoFactorCode { get; set; } = default!;\r\n\r\n   " +
                    "     /// <summary>\r\n        ///     This API supports the ASP.NET Core Identity " +
                    "default UI infrastructure and is not intended to be used\r\n        ///     direct" +
                    "ly from your code. This API may change or be removed in future releases.\r\n      " +
                    "  /// </summary>\r\n        [Display(Name = \"Remember this machine\")]\r\n        pub" +
                    "lic bool RememberMachine { get; set; }\r\n    }\r\n\r\n    public async Task<IActionRe" +
                    "sult> OnGetAsync(bool rememberMe, string? returnUrl = null)\r\n    {\r\n        // E" +
                    "nsure the user has gone through the username & password screen first\r\n        va" +
                    "r user = await _signInManager.GetTwoFactorAuthenticationUserAsync();\r\n\r\n        " +
                    "if (user == null)\r\n        {\r\n            throw new InvalidOperationException($\"" +
                    "Unable to load two-factor authentication user.\");\r\n        }\r\n\r\n        ReturnUr" +
                    "l = returnUrl;\r\n        RememberMe = rememberMe;\r\n\r\n        return Page();\r\n    " +
                    "}\r\n\r\n    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? r" +
                    "eturnUrl = null)\r\n    {\r\n        if (!ModelState.IsValid)\r\n        {\r\n          " +
                    "  return Page();\r\n        }\r\n\r\n        returnUrl = returnUrl ?? Url.Content(\"~/\"" +
                    ");\r\n\r\n        var user = await _signInManager.GetTwoFactorAuthenticationUserAsyn" +
                    "c();\r\n        if (user == null)\r\n        {\r\n            throw new InvalidOperati" +
                    "onException($\"Unable to load two-factor authentication user.\");\r\n        }\r\n\r\n  " +
                    "      var authenticatorCode = Input.TwoFactorCode.Replace(\" \", string.Empty).Rep" +
                    "lace(\"-\", string.Empty);\r\n\r\n        var result = await _signInManager.TwoFactorA" +
                    "uthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);\r\n" +
                    "\r\n        var userId = await _userManager.GetUserIdAsync(user);\r\n\r\n        if (r" +
                    "esult.Succeeded)\r\n        {\r\n            _logger.LogInformation(\"User with ID \'{" +
                    "UserId}\' logged in with 2fa.\", user.Id);\r\n            return LocalRedirect(retur" +
                    "nUrl);\r\n        }\r\n        else if (result.IsLockedOut)\r\n        {\r\n            " +
                    "_logger.LogWarning(\"User with ID \'{UserId}\' account locked out.\", user.Id);\r\n   " +
                    "         return RedirectToPage(\"./Lockout\");\r\n        }\r\n        else\r\n        {" +
                    "\r\n            _logger.LogWarning(\"Invalid authenticator code entered for user wi" +
                    "th ID \'{UserId}\'.\", user.Id);\r\n            ModelState.AddModelError(string.Empty" +
                    ", \"Invalid authenticator code.\");\r\n            return Page();\r\n        }\r\n    }\r" +
                    "\n}\r\n");
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
    public class LoginWith2faModelBase
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
