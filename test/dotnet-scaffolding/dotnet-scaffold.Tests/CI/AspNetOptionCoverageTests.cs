// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CI;

/// <summary>
/// Tests that validate every ASP.NET CLI option has correct metadata:
/// CliOption flag, DisplayName, Description, and Required semantics.
/// </summary>
public class AspNetOptionCoverageTests
{
    private readonly AspNetOptions _options = new();

    #region --project

    [Fact]
    public void Project_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.ProjectCliOption, _options.Project.CliOption);

    [Fact]
    public void Project_IsRequired()
        => Assert.True(_options.Project.Required);

    [Fact]
    public void Project_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(_options.Project.Description));

    [Fact]
    public void Project_HasNonEmptyDisplayName()
        => Assert.False(string.IsNullOrWhiteSpace(_options.Project.DisplayName));

    #endregion

    #region --prerelease

    [Fact]
    public void Prerelease_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.PrereleaseCliOption, _options.Prerelease.CliOption);

    [Fact]
    public void Prerelease_IsNotRequired()
        => Assert.False(_options.Prerelease.Required);

    [Fact]
    public void Prerelease_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(_options.Prerelease.Description));

    #endregion

    #region --name (FileName / AreaName)

    [Fact]
    public void FileName_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.NameOption, _options.FileName.CliOption);

    [Fact]
    public void FileName_IsRequired()
        => Assert.True(_options.FileName.Required);

    [Fact]
    public void AreaName_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.NameOption, _options.AreaName.CliOption);

    [Fact]
    public void AreaName_IsRequired()
        => Assert.True(_options.AreaName.Required);

    #endregion

    #region --actions

    [Fact]
    public void Actions_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.ActionsOption, _options.Actions.CliOption);

    [Fact]
    public void Actions_IsRequired()
        => Assert.True(_options.Actions.Required);

    #endregion

    #region --model

    [Fact]
    public void ModelName_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.ModelCliOption, _options.ModelName.CliOption);

    [Fact]
    public void ModelName_IsRequired()
        => Assert.True(_options.ModelName.Required);

    #endregion

    #region --endpoints

    [Fact]
    public void EndpointsClass_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.EndpointsOption, _options.EndpointsClass.CliOption);

    [Fact]
    public void EndpointsClass_IsRequired()
        => Assert.True(_options.EndpointsClass.Required);

    #endregion

    #region --dbProvider

    [Fact]
    public void DatabaseProvider_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.DbProviderOption, _options.DatabaseProvider.CliOption);

    [Fact]
    public void DatabaseProvider_IsNotRequired()
        => Assert.False(_options.DatabaseProvider.Required);

    [Fact]
    public void DatabaseProviderRequired_IsRequired()
        => Assert.True(_options.DatabaseProviderRequired.Required);

    [Fact]
    public void DatabaseProviderRequired_HasSameCliOption()
        => Assert.Equal(Constants.CliOptions.DbProviderOption, _options.DatabaseProviderRequired.CliOption);

    [Fact]
    public void IdentityDbProviderRequired_HasSameCliOption()
        => Assert.Equal(Constants.CliOptions.DbProviderOption, _options.IdentityDbProviderRequired.CliOption);

    [Fact]
    public void IdentityDbProviderRequired_IsRequired()
        => Assert.True(_options.IdentityDbProviderRequired.Required);

    [Fact]
    public void DatabaseProvider_HasCustomPickerValues()
        => Assert.NotEmpty(_options.DatabaseProvider.CustomPickerValues!);

    [Fact]
    public void DatabaseProviderRequired_HasCustomPickerValues()
        => Assert.NotEmpty(_options.DatabaseProviderRequired.CustomPickerValues!);

    [Fact]
    public void IdentityDbProviderRequired_HasCustomPickerValues()
        => Assert.NotEmpty(_options.IdentityDbProviderRequired.CustomPickerValues!);

    #endregion

    #region --dataContext

    [Fact]
    public void DataContextClass_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.DataContextOption, _options.DataContextClass.CliOption);

    [Fact]
    public void DataContextClass_IsNotRequired()
        => Assert.False(_options.DataContextClass.Required);

    [Fact]
    public void DataContextClassRequired_IsRequired()
        => Assert.True(_options.DataContextClassRequired.Required);

    [Fact]
    public void DataContextClassRequired_HasSameCliOption()
        => Assert.Equal(Constants.CliOptions.DataContextOption, _options.DataContextClassRequired.CliOption);

    #endregion

    #region --open (OpenApi)

    [Fact]
    public void OpenApi_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.OpenApiOption, _options.OpenApi.CliOption);

    [Fact]
    public void OpenApi_IsNotRequired()
        => Assert.False(_options.OpenApi.Required);

    #endregion

    #region --typedResults

    [Fact]
    public void TypedResults_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.TypedResultsOption, _options.TypedResults.CliOption);

    [Fact]
    public void TypedResults_IsNotRequired()
        => Assert.False(_options.TypedResults.Required);

    #endregion

    #region --page

    [Fact]
    public void PageType_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.PageTypeOption, _options.PageType.CliOption);

    [Fact]
    public void PageType_IsRequired()
        => Assert.True(_options.PageType.Required);

    [Fact]
    public void PageType_HasCustomPickerValues()
        => Assert.NotEmpty(_options.PageType.CustomPickerValues!);

    #endregion

    #region --controller

    [Fact]
    public void ControllerName_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.ControllerNameOption, _options.ControllerName.CliOption);

    [Fact]
    public void ControllerName_IsRequired()
        => Assert.True(_options.ControllerName.Required);

    #endregion

    #region --views

    [Fact]
    public void Views_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.ViewsOption, _options.Views.CliOption);

    [Fact]
    public void Views_IsRequired()
        => Assert.True(_options.Views.Required);

    #endregion

    #region --overwrite

    [Fact]
    public void Overwrite_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.OverwriteOption, _options.Overwrite.CliOption);

    [Fact]
    public void Overwrite_IsNotRequired()
        => Assert.False(_options.Overwrite.Required);

    #endregion

    #region --use-existing-application

    [Fact]
    public void UseExistingApplication_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.UseExistingApplicationOption, _options.UseExistingApplication.CliOption);

    [Fact]
    public void UseExistingApplication_IsRequired()
        => Assert.True(_options.UseExistingApplication.Required);

    #endregion

    #region --username

    [Fact]
    public void Username_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.UsernameOption, _options.Username.CliOption);

    [Fact]
    public void Username_IsRequired()
        => Assert.True(_options.Username.Required);

    #endregion

    #region --tenantId

    [Fact]
    public void TenantId_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.TenantIdOption, _options.TenantId.CliOption);

    [Fact]
    public void TenantId_IsRequired()
        => Assert.True(_options.TenantId.Required);

    #endregion

    #region --applicationId

    [Fact]
    public void ApplicationId_HasCorrectCliOption()
        => Assert.Equal(Constants.CliOptions.ApplicationIdOption, _options.ApplicationId.CliOption);

    [Fact]
    public void ApplicationId_IsNotRequired()
        => Assert.False(_options.ApplicationId.Required);

    #endregion
}
