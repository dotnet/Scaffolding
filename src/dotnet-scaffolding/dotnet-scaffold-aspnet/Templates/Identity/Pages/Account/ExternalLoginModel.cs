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
    public partial class ExternalLoginModel : ExternalLoginModelBase
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
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassNamespace));
            this.Write(";\r\n\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.IdentityNamespace));
            this.Write(".Pages.Account;\r\n\r\n[AllowAnonymous]\r\npublic class ExternalLoginModel : PageModel\r" +
                    "\n{\r\n    private readonly SignInManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _signInManager;\r\n    private readonly UserManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _userManager;\r\n    private readonly IUserStore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _userStore;\r\n    private readonly IUserEmailStore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> _emailStore;\r\n    private readonly IEmailSender _emailSender;\r\n    private read" +
                    "only ILogger<ExternalLoginModel> _logger;\r\n\r\n    public ExternalLoginModel(\r\n   " +
                    "     SignInManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> signInManager,\r\n        UserManager<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> userManager,\r\n        IUserStore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> userStore,\r\n        ILogger<ExternalLoginModel> logger,\r\n        IEmailSender e" +
                    "mailSender)\r\n    {\r\n        _signInManager = signInManager;\r\n        _userManage" +
                    "r = userManager;\r\n        _userStore = userStore;\r\n        _emailStore = GetEmai" +
                    "lStore();\r\n        _logger = logger;\r\n        _emailSender = emailSender;\r\n    }" +
                    "\r\n\r\n    /// <summary>\r\n    ///     This API supports the ASP.NET Core Identity d" +
                    "efault UI infrastructure and is not intended to be used\r\n    ///     directly fr" +
                    "om your code. This API may change or be removed in future releases.\r\n    /// </s" +
                    "ummary>\r\n    [BindProperty]\r\n    public InputModel Input { get; set; } = default" +
                    "!;\r\n\r\n    /// <summary>\r\n    ///     This API supports the ASP.NET Core Identity" +
                    " default UI infrastructure and is not intended to be used\r\n    ///     directly " +
                    "from your code. This API may change or be removed in future releases.\r\n    /// <" +
                    "/summary>\r\n    public string? ProviderDisplayName { get; set; }\r\n\r\n    /// <summ" +
                    "ary>\r\n    ///     This API supports the ASP.NET Core Identity default UI infrast" +
                    "ructure and is not intended to be used\r\n    ///     directly from your code. Thi" +
                    "s API may change or be removed in future releases.\r\n    /// </summary>\r\n    publ" +
                    "ic string? ReturnUrl { get; set; }\r\n\r\n    /// <summary>\r\n    ///     This API su" +
                    "pports the ASP.NET Core Identity default UI infrastructure and is not intended t" +
                    "o be used\r\n    ///     directly from your code. This API may change or be remove" +
                    "d in future releases.\r\n    /// </summary>\r\n    [TempData]\r\n    public string? Er" +
                    "rorMessage { get; set; }\r\n\r\n    /// <summary>\r\n    ///     This API supports the" +
                    " ASP.NET Core Identity default UI infrastructure and is not intended to be used\r" +
                    "\n    ///     directly from your code. This API may change or be removed in futur" +
                    "e releases.\r\n    /// </summary>\r\n    public class InputModel\r\n    {\r\n        ///" +
                    " <summary>\r\n        ///     This API supports the ASP.NET Core Identity default " +
                    "UI infrastructure and is not intended to be used\r\n        ///     directly from " +
                    "your code. This API may change or be removed in future releases.\r\n        /// </" +
                    "summary>\r\n        [Required]\r\n        [EmailAddress]\r\n        public string Emai" +
                    "l { get; set; } = default!;\r\n    }\r\n        \r\n    public IActionResult OnGet() =" +
                    "> RedirectToPage(\"./Login\");\r\n\r\n    public IActionResult OnPost(string provider," +
                    " string? returnUrl = null)\r\n    {\r\n        // Request a redirect to the external" +
                    " login provider.\r\n        var redirectUrl = Url.Page(\"./ExternalLogin\", pageHand" +
                    "ler: \"Callback\", values: new { returnUrl });\r\n        var properties = _signInMa" +
                    "nager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);\r\n       " +
                    " return new ChallengeResult(provider, properties);\r\n    }\r\n\r\n    public async Ta" +
                    "sk<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteErr" +
                    "or = null)\r\n    {\r\n        returnUrl = returnUrl ?? Url.Content(\"~/\");\r\n        " +
                    "if (remoteError != null)\r\n        {\r\n            ErrorMessage = $\"Error from ext" +
                    "ernal provider: {remoteError}\";\r\n            return RedirectToPage(\"./Login\", ne" +
                    "w { ReturnUrl = returnUrl });\r\n        }\r\n        var info = await _signInManage" +
                    "r.GetExternalLoginInfoAsync();\r\n        if (info == null)\r\n        {\r\n          " +
                    "  ErrorMessage = \"Error loading external login information.\";\r\n            retur" +
                    "n RedirectToPage(\"./Login\", new { ReturnUrl = returnUrl });\r\n        }\r\n\r\n      " +
                    "  // Sign in the user with this external login provider if the user already has " +
                    "a login.\r\n        var result = await _signInManager.ExternalLoginSignInAsync(inf" +
                    "o.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);\r" +
                    "\n        if (result.Succeeded)\r\n        {\r\n            _logger.LogInformation(\"{" +
                    "Name} logged in with {LoginProvider} provider.\", info.Principal.Identity?.Name, " +
                    "info.LoginProvider);\r\n            return LocalRedirect(returnUrl);\r\n        }\r\n " +
                    "       if (result.IsLockedOut)\r\n        {\r\n            return RedirectToPage(\"./" +
                    "Lockout\");\r\n        }\r\n        else\r\n        {\r\n            // If the user does " +
                    "not have an account, then ask the user to create an account.\r\n            Return" +
                    "Url = returnUrl;\r\n            ProviderDisplayName = info.ProviderDisplayName;\r\n " +
                    "           if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))\r\n      " +
                    "      {\r\n                Input = new InputModel\r\n                {\r\n            " +
                    "        Email = info.Principal.FindFirstValue(ClaimTypes.Email)!\r\n              " +
                    "  };\r\n            }\r\n            return Page();\r\n        }\r\n    }\r\n\r\n    public " +
                    "async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)\r\n   " +
                    " {\r\n        returnUrl = returnUrl ?? Url.Content(\"~/\");\r\n        // Get the info" +
                    "rmation about the user from the external login provider\r\n        var info = awai" +
                    "t _signInManager.GetExternalLoginInfoAsync();\r\n        if (info == null)\r\n      " +
                    "  {\r\n            ErrorMessage = \"Error loading external login information during" +
                    " confirmation.\";\r\n            return RedirectToPage(\"./Login\", new { ReturnUrl =" +
                    " returnUrl });\r\n        }\r\n\r\n        if (ModelState.IsValid)\r\n        {\r\n       " +
                    "     var user = CreateUser();\r\n\r\n            await _userStore.SetUserNameAsync(u" +
                    "ser, Input.Email, CancellationToken.None);\r\n            await _emailStore.SetEma" +
                    "ilAsync(user, Input.Email, CancellationToken.None);\r\n\r\n            var result = " +
                    "await _userManager.CreateAsync(user);\r\n            if (result.Succeeded)\r\n      " +
                    "      {\r\n                result = await _userManager.AddLoginAsync(user, info);\r" +
                    "\n                if (result.Succeeded)\r\n                {\r\n                    _" +
                    "logger.LogInformation(\"User created an account using {Name} provider.\", info.Log" +
                    "inProvider);\r\n\r\n                    var userId = await _userManager.GetUserIdAsy" +
                    "nc(user);\r\n                    var code = await _userManager.GenerateEmailConfir" +
                    "mationTokenAsync(user);\r\n                    code = WebEncoders.Base64UrlEncode(" +
                    "Encoding.UTF8.GetBytes(code));\r\n                    var callbackUrl = Url.Page(\r" +
                    "\n                        \"/Account/ConfirmEmail\",\r\n                        pageH" +
                    "andler: null,\r\n                        values: new { area = \"Identity\", userId =" +
                    " userId, code = code },\r\n                        protocol: Request.Scheme)!;\r\n\r\n" +
                    "                    await _emailSender.SendEmailAsync(Input.Email, \"Confirm your" +
                    " email\",\r\n                        $\"Please confirm your account by <a href=\'{Htm" +
                    "lEncoder.Default.Encode(callbackUrl)}\'>clicking here</a>.\");\r\n\r\n                " +
                    "    // If account confirmation is required, we need to show the link if we don\'t" +
                    " have a real email sender\r\n                    if (_userManager.Options.SignIn.R" +
                    "equireConfirmedAccount)\r\n                    {\r\n                        return R" +
                    "edirectToPage(\"./RegisterConfirmation\", new { Email = Input.Email });\r\n         " +
                    "           }\r\n\r\n                    await _signInManager.SignInAsync(user, isPer" +
                    "sistent: false, info.LoginProvider);\r\n                    return LocalRedirect(r" +
                    "eturnUrl);\r\n                }\r\n            }\r\n            foreach (var error in " +
                    "result.Errors)\r\n            {\r\n                ModelState.AddModelError(string.E" +
                    "mpty, error.Description);\r\n            }\r\n        }\r\n\r\n        ProviderDisplayNa" +
                    "me = info.ProviderDisplayName;\r\n        ReturnUrl = returnUrl;\r\n        return P" +
                    "age();\r\n    }\r\n\r\n    private ");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write(" CreateUser()\r\n    {\r\n        try\r\n        {\r\n            return Activator.Create" +
                    "Instance<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write(">();\r\n        }\r\n        catch\r\n        {\r\n            throw new InvalidOperation" +
                    "Exception($\"Can\'t create an instance of \'{nameof(");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write(")}\'. \" +\r\n                $\"Ensure that \'{nameof(");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write(")}\' is not an abstract class and has a parameterless constructor, or alternativel" +
                    "y \" +\r\n                $\"override the external login page in /Areas/Identity/Pag" +
                    "es/Account/ExternalLogin.cshtml\");\r\n        }\r\n    }\r\n\r\n    private IUserEmailSt" +
                    "ore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write("> GetEmailStore()\r\n    {\r\n        if (!_userManager.SupportsUserEmail)\r\n        {" +
                    "\r\n            throw new NotSupportedException(\"The default UI requires a user st" +
                    "ore with email support.\");\r\n        }\r\n        return (IUserEmailStore<");
            this.Write(this.ToStringHelper.ToStringWithCulture(Model.UserClassName));
            this.Write(">)_userStore;\r\n    }\r\n}\r\n");
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
    public class ExternalLoginModelBase
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