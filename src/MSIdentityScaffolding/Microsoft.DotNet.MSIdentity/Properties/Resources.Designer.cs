﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.DotNet.MSIdentity.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.DotNet.MSIdentity.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to @page &quot;/callwebapi&quot;
        ///
        ///@using Microsoft.Identity.Web
        ///
        ///@inject IDownstreamWebApi downstreamAPI
        ///@inject MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler
        ///
        ///&lt;h1&gt;Call an API&lt;/h1&gt;
        ///
        ///&lt;p&gt;This component demonstrates fetching data from a Web API.&lt;/p&gt;
        ///
        ///@if (apiResult == null)
        ///{
        ///    &lt;p&gt;&lt;em&gt;Loading...&lt;/em&gt;&lt;/p&gt;
        ///}
        ///else
        ///{
        ///    &lt;h2&gt;API Result&lt;/h2&gt;
        ///    @apiResult
        ///}
        ///
        ///@code {
        ///    private HttpResponseMessage response;
        ///    private string apiResult;
        ///
        ///    protected override async Task OnInitia [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string add_dotnet_blazorserver_CallWebApi_razor {
            get {
                return ResourceManager.GetString("add_dotnet_blazorserver_CallWebApi_razor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;AuthorizeView&gt;
        ///    &lt;Authorized&gt;
        ///        Hello, @context.User.Identity?.Name!
        ///        &lt;a href=&quot;MicrosoftIdentity/Account/SignOut&quot;&gt;Log out&lt;/a&gt;
        ///    &lt;/Authorized&gt;
        ///    &lt;NotAuthorized&gt;
        ///        &lt;a href=&quot;MicrosoftIdentity/Account/SignIn&quot;&gt;Log in&lt;/a&gt;
        ///    &lt;/NotAuthorized&gt;
        ///&lt;/AuthorizeView&gt;
        ///.
        /// </summary>
        internal static string add_dotnet_blazorserver_LoginDisplay_razor {
            get {
                return ResourceManager.GetString("add_dotnet_blazorserver_LoginDisplay_razor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to @page &quot;/showprofile&quot;
        ///
        ///@using Microsoft.Identity.Web
        ///@using Microsoft.Graph
        ///@inject Microsoft.Graph.GraphServiceClient GraphServiceClient
        ///@inject MicrosoftIdentityConsentAndConditionalAccessHandler ConsentHandler
        ///
        ///&lt;h1&gt;Me&lt;/h1&gt;
        ///
        ///&lt;p&gt;This component demonstrates fetching data from a service.&lt;/p&gt;
        ///
        ///@if (user == null)
        ///{
        ///    &lt;p&gt;&lt;em&gt;Loading...&lt;/em&gt;&lt;/p&gt;
        ///}
        ///else
        ///{
        ///    &lt;table class=&quot;table table-striped table-condensed&quot; style=&quot;font-family: monospace&quot;&gt;
        ///        &lt;tr&gt;
        ///            &lt;th&gt;Property&lt;/th&gt;
        ///          [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string add_dotnet_blazorserver_ShowProfile_razor {
            get {
                return ResourceManager.GetString("add_dotnet_blazorserver_ShowProfile_razor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to @page &quot;/authentication/{action}&quot;
        ///@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
        ///&lt;RemoteAuthenticatorView Action=&quot;@Action&quot; /&gt;
        ///
        ///@code{
        ///    [Parameter] public string? Action { get; set; }
        ///}
        ///.
        /// </summary>
        internal static string add_dotnet_blazorwasm_Authentication_razor {
            get {
                return ResourceManager.GetString("add_dotnet_blazorwasm_Authentication_razor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Net.Http;
        ///using System.Net.Http.Headers;
        ///using System.Threading;
        ///using System.Threading.Tasks;
        ///using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
        ///using Microsoft.Authentication.WebAssembly.Msal.Models;
        ///using Microsoft.Extensions.DependencyInjection;
        ///using Microsoft.Graph;
        ///
        ////// &lt;summary&gt;
        ////// Adds services and implements methods to use Microsoft Graph SDK.
        ////// &lt;/summary&gt;
        ///internal static class GraphClientExtensions
        ///{
        ///    /// &lt;summary&gt;
        ///    /// Extension  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string add_dotnet_blazorwasm_GraphClientExtensions_cs {
            get {
                return ResourceManager.GetString("add_dotnet_blazorwasm_GraphClientExtensions_cs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to @using Microsoft.AspNetCore.Components.Authorization
        ///@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
        ///
        ///@inject NavigationManager Navigation
        ///@inject SignOutSessionStateManager SignOutManager
        ///
        ///&lt;AuthorizeView&gt;
        ///    &lt;Authorized&gt;
        ///        Hello, @context.User.Identity?.Name!
        ///        &lt;button class=&quot;nav-link btn btn-link&quot; @onclick=&quot;BeginLogout&quot;&gt;Log out&lt;/button&gt;
        ///    &lt;/Authorized&gt;
        ///    &lt;NotAuthorized&gt;
        ///        &lt;a href=&quot;authentication/login&quot;&gt;Log in&lt;/a&gt;
        ///    &lt;/NotAuthorized&gt;
        ///&lt;/AuthorizeView&gt;        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string add_dotnet_blazorwasm_LoginDisplay_razor {
            get {
                return ResourceManager.GetString("add_dotnet_blazorwasm_LoginDisplay_razor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to @inject NavigationManager Navigation
        ///
        ///@code {
        ///    protected override void OnInitialized()
        ///    {
        ///        Navigation.NavigateTo($&quot;authentication/login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}&quot;);
        ///    }
        ///}
        ///.
        /// </summary>
        internal static string add_dotnet_blazorwasm_RedirectToLogin_razor {
            get {
                return ResourceManager.GetString("add_dotnet_blazorwasm_RedirectToLogin_razor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to @page &quot;/profile&quot;
        ///@using Microsoft.AspNetCore.Authorization
        ///@using Microsoft.Graph
        ///@inject Microsoft.Graph.GraphServiceClient GraphServiceClient
        ///@attribute [Authorize]
        ///
        ///&lt;h3&gt;User Profile&lt;/h3&gt;
        ///@if (user == null)
        ///{
        ///    &lt;p&gt;&lt;em&gt;Loading...&lt;/em&gt;&lt;/p&gt;
        ///}
        ///else
        ///{
        ///    &lt;table class=&quot;table&quot;&gt;
        ///        &lt;thead&gt;
        ///            &lt;tr&gt;
        ///                &lt;th&gt;Property&lt;/th&gt;
        ///                &lt;th&gt;Value&lt;/th&gt;
        ///            &lt;/tr&gt;
        ///        &lt;/thead&gt;
        ///        &lt;tr&gt;
        ///            &lt;td&gt; DisplayName &lt;/td&gt;
        ///            &lt;td&gt; @user.DisplayNa [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string add_dotnet_blazorwasm_UserProfile_razor {
            get {
                return ResourceManager.GetString("add_dotnet_blazorwasm_UserProfile_razor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Added code file {0}.
        /// </summary>
        internal static string AddedCodeFile {
            get {
                return ResourceManager.GetString("AddedCodeFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Added {0} to user secrets..
        /// </summary>
        internal static string AddingKeyToUserSecrets {
            get {
                return ResourceManager.GetString("AddingKeyToUserSecrets", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adding package {0} . . ..
        /// </summary>
        internal static string AddingPackage {
            get {
                return ResourceManager.GetString("AddingPackage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Couldn&apos;t find app {0} in tenant {1}.
        /// </summary>
        internal static string AppNotFound {
            get {
                return ResourceManager.GetString("AppNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Authentication is not enabled yet in this project. An app registration will be created, but the tool does not add the code yet (work in progress)..
        /// </summary>
        internal static string AuthNotEnabled {
            get {
                return ResourceManager.GetString("AuthNotEnabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client secret - {0}..
        /// </summary>
        internal static string ClientSecret {
            get {
                return ResourceManager.GetString("ClientSecret", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] cm_dotnet_blazorserver {
            get {
                object obj = ResourceManager.GetObject("cm_dotnet_blazorserver", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] cm_dotnet_blazorwasm {
            get {
                object obj = ResourceManager.GetObject("cm_dotnet_blazorwasm", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] cm_dotnet_blazorwasm_client {
            get {
                object obj = ResourceManager.GetObject("cm_dotnet_blazorwasm_client", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] cm_dotnet_blazorwasm_hosted {
            get {
                object obj = ResourceManager.GetObject("cm_dotnet_blazorwasm_hosted", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] cm_dotnet_minimal_api {
            get {
                object obj = ResourceManager.GetObject("cm_dotnet_minimal_api", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] cm_dotnet_webapi {
            get {
                object obj = ResourceManager.GetObject("cm_dotnet_webapi", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] cm_dotnet_webapp {
            get {
                object obj = ResourceManager.GetObject("cm_dotnet_webapp", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error parsing Code Modifier Config for project type {0}, exception: {1}.
        /// </summary>
        internal static string CodeModifierConfigParsingError {
            get {
                return ResourceManager.GetString("CodeModifierConfigParsingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Configured Blazor WASM client app registration &quot;{0}&quot; ({1}).
        /// </summary>
        internal static string ConfiguredBlazorWasmClient {
            get {
                return ResourceManager.GetString("ConfiguredBlazorWasmClient", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Created app {0} - {1}..
        /// </summary>
        internal static string CreatedAppRegistration {
            get {
                return ResourceManager.GetString("CreatedAppRegistration", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Detected project type {0}..
        /// </summary>
        internal static string DetectedProjectType {
            get {
                return ResourceManager.GetString("DetectedProjectType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_blazor_server {
            get {
                object obj = ResourceManager.GetObject("dotnet_blazor_server", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_blazorwasm {
            get {
                object obj = ResourceManager.GetObject("dotnet_blazorwasm", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_blazorwasm_client {
            get {
                object obj = ResourceManager.GetObject("dotnet_blazorwasm_client", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_blazorwasm_hosted {
            get {
                object obj = ResourceManager.GetObject("dotnet_blazorwasm_hosted", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_minimal_api {
            get {
                object obj = ResourceManager.GetObject("dotnet_minimal_api", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_web {
            get {
                object obj = ResourceManager.GetObject("dotnet_web", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_webapi {
            get {
                object obj = ResourceManager.GetObject("dotnet_webapi", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] dotnet_webapp {
            get {
                object obj = ResourceManager.GetObject("dotnet_webapp", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while running dotnet-user-secrets init.
        /// </summary>
        internal static string DotnetUserSecretsError {
            get {
                return ResourceManager.GetString("DotnetUserSecretsError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exposing API scopes for Server App Registration &quot;{0}&quot; ({1}).
        /// </summary>
        internal static string ExposingScopes {
            get {
                return ResourceManager.GetString("ExposingScopes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FAILED\n\n.
        /// </summary>
        internal static string Failed {
            get {
                return ResourceManager.GetString("Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to add package {0}.
        /// </summary>
        internal static string FailedAddPackage {
            get {
                return ResourceManager.GetString("FailedAddPackage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to add client secret..
        /// </summary>
        internal static string FailedClientSecret {
            get {
                return ResourceManager.GetString("FailedClientSecret", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to add client secret for Azure AD app : {0}({1}).
        /// </summary>
        internal static string FailedClientSecretWithApp {
            get {
                return ResourceManager.GetString("FailedClientSecretWithApp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to acquire a token..
        /// </summary>
        internal static string FailedToAcquireToken {
            get {
                return ResourceManager.GetString("FailedToAcquireToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to create Azure AD/AD B2C app registration..
        /// </summary>
        internal static string FailedToCreateApp {
            get {
                return ResourceManager.GetString("FailedToCreateApp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to get or create service principal..
        /// </summary>
        internal static string FailedToGetServicePrincipal {
            get {
                return ResourceManager.GetString("FailedToGetServicePrincipal", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to modify code file {0}, {1} .
        /// </summary>
        internal static string FailedToModifyCodeFile {
            get {
                return ResourceManager.GetString("FailedToModifyCodeFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to provision Client Application for Blazor WASM hosted project.
        /// </summary>
        internal static string FailedToProvisionClientApp {
            get {
                return ResourceManager.GetString("FailedToProvisionClientApp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to retrieve all Azure AD/AD B2C objects(apps/service principals.
        /// </summary>
        internal static string FailedToRetrieveADObjectsError {
            get {
                return ResourceManager.GetString("FailedToRetrieveADObjectsError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to retrieve application parameters..
        /// </summary>
        internal static string FailedToRetrieveApplicationParameters {
            get {
                return ResourceManager.GetString("FailedToRetrieveApplicationParameters", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to update Azure AD app registration {0} ({1}).
        /// </summary>
        internal static string FailedToUpdateApp {
            get {
                return ResourceManager.GetString("FailedToUpdateApp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to update Azure AD app, null {0}.
        /// </summary>
        internal static string FailedToUpdateAppNull {
            get {
                return ResourceManager.GetString("FailedToUpdateAppNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to update client app program.cs file for Blazor WASM hosted project.
        /// </summary>
        internal static string FailedToUpdateClientAppCode {
            get {
                return ResourceManager.GetString("FailedToUpdateClientAppCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initializing User Secrets . . ..
        /// </summary>
        internal static string InitializeUserSecrets {
            get {
                return ResourceManager.GetString("InitializeUserSecrets", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You&apos;ll need to remove the calls to Microsoft Graph as it&apos;s not supported by B2C apps..
        /// </summary>
        internal static string MicrosoftGraphNotSupported {
            get {
                return ResourceManager.GetString("MicrosoftGraphNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Config identifier: {0} does not match toolOptions identifier: {1}.
        /// </summary>
        internal static string MismatchedProjectTypeIdentifier {
            get {
                return ResourceManager.GetString("MismatchedProjectTypeIdentifier", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Modified code file {0}.
        /// </summary>
        internal static string ModifiedCodeFile {
            get {
                return ResourceManager.GetString("ModifiedCodeFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No valid project description found with project type identifier &quot;{0}&quot;.
        /// </summary>
        internal static string NoProjectDescriptionFound {
            get {
                return ResourceManager.GetString("NoProjectDescriptionFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No project found in {0}..
        /// </summary>
        internal static string NoProjectFound {
            get {
                return ResourceManager.GetString("NoProjectFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not find app registration with matching ID in Azure AD (ID: {0}).
        /// </summary>
        internal static string NotFound {
            get {
                return ResourceManager.GetString("NotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Azure AD app {0} ({1}) did not require any remote updates.
        /// </summary>
        internal static string NoUpdateNecessary {
            get {
                return ResourceManager.GetString("NoUpdateNecessary", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;SignedOutCallbackPath&quot;: &quot;/signout/B2C_1_susi&quot;,
        ///    &quot;SignUpSignInPolicyId&quot;: &quot;b2c_1_susi&quot;,
        ///    &quot;ResetPasswordPolicyId&quot;: &quot;b2c_1_reset&quot;,
        ///    &quot;EditProfilePolicyId&quot;: &quot;b2c_1_edit_profile&quot;,
        ///.
        /// </summary>
        internal static string Policies {
            get {
                return ResourceManager.GetString("Policies", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to validate project path {0}.
        /// </summary>
        internal static string ProjectPathError {
            get {
                return ResourceManager.GetString("ProjectPathError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Resource file {0} could not be parsed..
        /// </summary>
        internal static string ResourceFileParseError {
            get {
                return ResourceManager.GetString("ResourceFileParseError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error encountered with sign-in. See error message for details:.
        /// </summary>
        internal static string SignInError {
            get {
                return ResourceManager.GetString("SignInError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SUCCESS.
        /// </summary>
        internal static string Success {
            get {
                return ResourceManager.GetString("Success", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Successfully updated app registration {0} ({1}).
        /// </summary>
        internal static string SuccessfullyUpdatedApp {
            get {
                return ResourceManager.GetString("SuccessfullyUpdatedApp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Summary.
        /// </summary>
        internal static string Summary {
            get {
                return ResourceManager.GetString("Summary", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updating appsettings.json.
        /// </summary>
        internal static string UpdatingAppSettingsJson {
            get {
                return ResourceManager.GetString("UpdatingAppSettingsJson", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updating project files ....
        /// </summary>
        internal static string UpdatingProjectFiles {
            get {
                return ResourceManager.GetString("UpdatingProjectFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updating project packages ....
        /// </summary>
        internal static string UpdatingProjectPackages {
            get {
                return ResourceManager.GetString("UpdatingProjectPackages", resourceCulture);
            }
        }
    }
}
