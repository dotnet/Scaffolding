// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.DotNet.MSIdentity.Shared;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    /// <summary>
    /// ProvisioningToolOptionsBinder to setup handlers for dotnet-msidentity commnds.
    /// Used in Microsoft.DotNet.MSIdentity.Tool.Program in SetHandler calls.
    /// </summary>
    internal class ProvisioningToolOptionsBinder
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

        public ProvisioningToolOptions GetBoundValue(ParseResult parseResult)
        {
            IList<string> redirectUriList = parseResult.GetValue(_redirectUriOption) ?? new List<string>();
            return new ProvisioningToolOptions
            {
                HostedApiScopes = parseResult.GetValue(_hostedAppIdUriOption),
                CallsDownstreamApi = parseResult.GetValue(_callsDownstreamApiOption),
                UpdateUserSecrets = parseResult.GetValue(_updateUserSecretsOption),
                CallsGraph = parseResult.GetValue(_callsGraphOption),
                ClientId = parseResult.GetValue(_clientIdOption),
                ClientSecret = parseResult.GetValue(_clientSecretOption),
                ProjectType = parseResult.GetValue(_projectTypeOption),
                SusiPolicyId = parseResult.GetValue(_susiPolicyIdOption),
                TenantId = parseResult.GetValue(_tenantOption),
                Username = parseResult.GetValue(_usernameOption),
                ProjectFilePath = parseResult.GetValue(_projectFilePathOption),
                WebApiClientId = parseResult.GetValue(_apiClientIdOption),
                HostedAppIdUri = parseResult.GetValue(_hostedAppIdUriOption),
                Json = parseResult.GetValue(_jsonOption),
                AppDisplayName = parseResult.GetValue(_appDisplayName),
                RedirectUris = redirectUriList,
                EnableIdToken = parseResult.GetValue(_enableIdTokenOption),
                EnableAccessToken = parseResult.GetValue(_enableAccessToken),
                ConfigUpdate = parseResult.GetValue(_configUpdateOption),
                CodeUpdate = parseResult.GetValue(_codeUpdateOption),
                PackagesUpdate = parseResult.GetValue(_packagesUpdateOption),
                ApiScopes = parseResult.GetValue(_apiScopesOption),
                ClientProject = parseResult.GetValue(_clientProjectOption),
                Instance = parseResult.GetValue(_instanceOption),
                CalledApiUrl = parseResult.GetValue(_calledApiUrlOption)
            };
        }
    }
}
