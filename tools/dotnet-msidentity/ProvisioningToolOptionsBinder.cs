// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    /// <summary>
    /// ProvisioningToolOptionsBinder to setup handlers for dotnet-msidentity commnds.
    /// Used in Microsoft.DotNet.MSIdentity.Tool.Program in SetHandler calls.
    /// </summary>
    internal class ProvisioningToolOptionsBinder : BinderBase<ProvisioningToolOptions>
    {
        private readonly Option<bool> _jsonOption;
        private readonly Option<bool> _enableIdTokenOption;
        private readonly Option<bool> _enableAccessToken;
        private readonly Option<bool> _callsGraphOption;
        private readonly Option<bool> _callsDownstreamApiOption;
        private readonly Option<bool> _updateUserSecretsOption;
        private readonly Option<bool> _configUpdateOption;
        private readonly Option<bool> _codeUpdateOption;
        private readonly Option<bool> _packagesUpdateOption;
        private readonly Option<string> _clientIdOption;
        private readonly Option<string> _appDisplayName;
        private readonly Option<string> _projectTypeOption;
        private readonly Option<string> _clientSecretOption;
        private readonly Option<IList<string>> _redirectUriOption;
        private readonly Option<string> _projectFilePathOption;
        private readonly Option<string> _clientProjectOption;
        private readonly Option<string> _apiScopesOption;
        private readonly Option<string> _hostedAppIdUriOption;
        private readonly Option<string> _apiClientIdOption;
        private readonly Option<string> _susiPolicyIdOption;
        private readonly Option<string> _tenantOption;
        private readonly Option<string> _usernameOption;
        private readonly Option<string> _instanceOption;
        private readonly Option<string> _calledApiUrlOption;

        public ProvisioningToolOptionsBinder(
            Option<bool> jsonOption,
            Option<bool> enableIdTokenOption,
            Option<bool> enableAccessToken,
            Option<bool> callsGraphOption,
            Option<bool> callsDownstreamApiOption,
            Option<bool> updateUserSecretsOption,
            Option<bool> configUpdateOption,
            Option<bool> codeUpdateOption,
            Option<bool> packagesUpdateOption,
            Option<string> clientIdOption,
            Option<string> appDisplayName,
            Option<string> projectTypeOption,
            Option<string> clientSecretOption,
            Option<IList<string>> redirectUriOption,
            Option<string> projectFilePathOption,
            Option<string> clientProjectOption,
            Option<string> apiScopesOption,
            Option<string> hostedAppIdUriOption,
            Option<string> apiClientIdOption,
            Option<string> susiPolicyIdOption,
            Option<string> tenantOption,
            Option<string> usernameOption,
            Option<string> instanceOption,
            Option<string> calledApiUrlOption)
        {
            _jsonOption = jsonOption;
            _enableIdTokenOption = enableIdTokenOption;
            _enableAccessToken = enableAccessToken;
            _callsGraphOption = callsGraphOption;
            _callsDownstreamApiOption = callsDownstreamApiOption;
            _updateUserSecretsOption = updateUserSecretsOption;
            _configUpdateOption = configUpdateOption;
            _codeUpdateOption = codeUpdateOption;
            _packagesUpdateOption = packagesUpdateOption;
            _clientIdOption = clientIdOption;
            _appDisplayName = appDisplayName;
            _projectTypeOption = projectTypeOption;
            _clientSecretOption = clientSecretOption;
            _redirectUriOption = redirectUriOption;
            _projectFilePathOption = projectFilePathOption;
            _clientProjectOption = clientProjectOption;
            _apiScopesOption = apiScopesOption;
            _hostedAppIdUriOption = hostedAppIdUriOption;
            _apiClientIdOption = apiClientIdOption;
            _susiPolicyIdOption = susiPolicyIdOption;
            _tenantOption = tenantOption;
            _usernameOption = usernameOption;
            _instanceOption = instanceOption;
            _calledApiUrlOption = calledApiUrlOption;
        }

        protected override ProvisioningToolOptions GetBoundValue(BindingContext bindingContext)
        {
            IList<string> redirectUriList = bindingContext.ParseResult.GetValue(_redirectUriOption) ?? new List<string>();
            return new ProvisioningToolOptions
            {
                HostedApiScopes = bindingContext.ParseResult.GetValue(_hostedAppIdUriOption),
                CallsDownstreamApi = bindingContext.ParseResult.GetValue(_callsDownstreamApiOption),
                UpdateUserSecrets = bindingContext.ParseResult.GetValue(_updateUserSecretsOption),
                CallsGraph = bindingContext.ParseResult.GetValue(_callsGraphOption),
                ClientId = bindingContext.ParseResult.GetValue(_clientIdOption),
                ClientSecret = bindingContext.ParseResult.GetValue(_clientSecretOption),
                ProjectType = bindingContext.ParseResult.GetValue(_projectTypeOption),
                SusiPolicyId = bindingContext.ParseResult.GetValue(_susiPolicyIdOption),
                TenantId = bindingContext.ParseResult.GetValue(_tenantOption),
                Username = bindingContext.ParseResult.GetValue(_usernameOption),
                ProjectFilePath = bindingContext.ParseResult.GetValue(_projectFilePathOption),
                WebApiClientId = bindingContext.ParseResult.GetValue(_apiClientIdOption),
                HostedAppIdUri = bindingContext.ParseResult.GetValue(_hostedAppIdUriOption),
                Json = bindingContext.ParseResult.GetValue(_jsonOption),
                AppDisplayName = bindingContext.ParseResult.GetValue(_appDisplayName),
                RedirectUris = redirectUriList,
                EnableIdToken = bindingContext.ParseResult.GetValue(_enableIdTokenOption),
                EnableAccessToken = bindingContext.ParseResult.GetValue(_enableAccessToken),
                ConfigUpdate = bindingContext.ParseResult.GetValue(_configUpdateOption),
                CodeUpdate = bindingContext.ParseResult.GetValue(_codeUpdateOption),
                PackagesUpdate = bindingContext.ParseResult.GetValue(_packagesUpdateOption),
                ApiScopes = bindingContext.ParseResult.GetValue(_apiScopesOption),
                ClientProject = bindingContext.ParseResult.GetValue(_clientProjectOption),
                Instance = bindingContext.ParseResult.GetValue(_instanceOption),
                CalledApiUrl = bindingContext.ParseResult.GetValue(_calledApiUrlOption)
            };
        }
    }
}
