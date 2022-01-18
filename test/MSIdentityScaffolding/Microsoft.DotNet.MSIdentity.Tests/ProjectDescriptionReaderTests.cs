// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.DotNet.MSIdentity.CodeReaderWriter;
using Microsoft.DotNet.MSIdentity.Project;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class ProjectDescriptionReaderTests
    {
        private readonly ITestOutputHelper _testOutput;

        public ProjectDescriptionReaderTests(ITestOutputHelper output)
        {
            _testOutput = output;
        }

        readonly ProjectDescriptionReader _projectDescriptionReader = new ProjectDescriptionReader();
        readonly CodeReader _codeReader = new CodeReader();

        [InlineData(@"blazorserver\blazorserver-b2c", "dotnet new blazorserver --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com", "dotnet-webapp", true)]
        [InlineData(@"blazorserver\blazorserver-b2c-callswebapi", "dotnet new blazorserver --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"", "dotnet-webapp", true)]
        [InlineData(@"blazorserver\blazorserver-singleorg", "dotnet new blazorserver --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com", "dotnet-webapp")]
        [InlineData(@"blazorserver\blazorserver-singleorg-callsgraph", "dotnet new blazorserver --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --calls-graph", "dotnet-webapp")]
        [InlineData(@"blazorserver\blazorserver-singleorg-callswebapi", "dotnet new blazorserver --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"", "dotnet-webapp")]
        [InlineData(@"mvc\mvc-b2c", "dotnet new mvc --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com", "dotnet-webapp", true)]
        [InlineData(@"mvc\mvc-b2c-callswebapi", "dotnet new mvc --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"", "dotnet-webapp", true)]
        [InlineData(@"mvc\mvc-singleorg", "dotnet new mvc --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com", "dotnet-webapp")]
        [InlineData(@"mvc\mvc-singleorg-callsgraph", "dotnet new mvc --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --calls-graph", "dotnet-webapp")]
        [InlineData(@"mvc\mvc-singleorg-callswebapi", "dotnet new mvc --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"", "dotnet-webapp")]
        [InlineData(@"webapi\webapi-b2c", "dotnet new webapi --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com", "dotnet-webapi", true)]
        [InlineData(@"webapi\webapi-singleorg", "dotnet new webapi --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com", "dotnet-webapi")]
        [InlineData(@"webapi\webapi-singleorg-callsgraph", "dotnet new webapi --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --calls-graph", "dotnet-webapi")]
        [InlineData(@"webapi\webapi-singleorg-callswebapi", "dotnet new webapi --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"", "dotnet-webapi")]
        [InlineData(@"webapp\webapp-b2c", "dotnet new webapp --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com", "dotnet-webapp", true)]
        [InlineData(@"webapp\webapp-b2c-callswebapi", "dotnet new webapp --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"", "dotnet-webapp", true)]
        [InlineData(@"webapp\webapp-singleorg", "dotnet new webapp --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com", "dotnet-webapp")]
        [InlineData(@"webapp\webapp-singleorg-callsgraph", "dotnet new webapp --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --calls-graph", "dotnet-webapp")]
        [InlineData(@"webapp\webapp-singleorg-callswebapi", "dotnet new webapp --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"", "dotnet-webapp")]
        [Theory(Skip="Test gets stuck on macOS and Linux. Tracking https://github.com/dotnet/Scaffolding/issues/1598 for fix.")]
        public void TestProjectDescriptionReader(string folderPath, string command, string expectedProjectType, bool isB2C = false)
        {
            string createdProjectFolder = CreateProjectIfNeeded(folderPath, command, "ProjectDescriptionReaderTests");

            var projectDescription = _projectDescriptionReader.GetProjectDescription(string.Empty, createdProjectFolder);

            Assert.NotNull(projectDescription);
            Assert.Equal(expectedProjectType, projectDescription.Identifier);

            var authenticationSettings = _codeReader.ReadFromFiles(
                createdProjectFolder,
                projectDescription,
                _projectDescriptionReader.ProjectDescriptions);

            bool callsGraph = folderPath.Contains(TestConstants.CallsGraph);
            bool callsWebApi = folderPath.Contains(TestConstants.CallsWebApi) || callsGraph;

            if (isB2C)
            {
                AssertAuthSettings(authenticationSettings, isB2C);
                Assert.Equal(TestConstants.B2CInstance, authenticationSettings.ApplicationParameters.Instance);

                if (callsWebApi)
                {
                    Assert.Equal(TestConstants.B2CScopes, authenticationSettings.ApplicationParameters.CalledApiScopes);
                }
            }
            else
            {
                AssertAuthSettings(authenticationSettings);

                if (callsWebApi)
                {
                    Assert.Equal(TestConstants.Scopes, authenticationSettings.ApplicationParameters.CalledApiScopes);
                }
            }

            Assert.Equal(callsGraph, authenticationSettings.ApplicationParameters.CallsMicrosoftGraph);
            Assert.Equal(callsWebApi, authenticationSettings.ApplicationParameters.CallsDownstreamApi);
        }

        [InlineData(@"blazorwasm\blazorwasm-b2c", "dotnet new blazorwasm --framework net5.0 --auth IndividualB2C --client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com", "dotnet-blazorwasm", true)]
        [InlineData(@"blazorwasm\blazorwasm-singleorg", "dotnet new blazorwasm --framework net5.0 --auth SingleOrg --client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75", "dotnet-blazorwasm")]
        [Theory(Skip="The newly created test project wants new packages that don't exist on official feeds. Will rework for next update.")]
        public void TestProjectDescriptionReader_TemplatesWithBlazorWasm(string folderPath, string command, string expectedProjectType, bool isB2C = false)
        {
            string createdProjectFolder = CreateProjectIfNeeded(folderPath, command, "ProjectDescriptionReaderTests");

            var projectDescription = _projectDescriptionReader.GetProjectDescription(string.Empty, createdProjectFolder);

            Assert.NotNull(projectDescription);
            Assert.Equal(expectedProjectType, projectDescription.Identifier);

            var authenticationSettings = _codeReader.ReadFromFiles(
                createdProjectFolder,
                projectDescription,
                _projectDescriptionReader.ProjectDescriptions);

            if (isB2C)
            {
                AssertAuthSettings(authenticationSettings, isB2C);
            }
            else
            {
                Assert.True(authenticationSettings.ApplicationParameters.HasAuthentication);
                Assert.True(authenticationSettings.ApplicationParameters.IsAAD);
                Assert.Null(authenticationSettings.ApplicationParameters.Instance);
                Assert.Equal(TestConstants.ClientId, authenticationSettings.ApplicationParameters.ClientId);
                Assert.Equal(TestConstants.DefaultDomain, authenticationSettings.ApplicationParameters.Domain);
                Assert.Equal(TestConstants.DefaultDomain, authenticationSettings.ApplicationParameters.Domain1);
                Assert.Equal(TestConstants.BlazorWasmAuthority, authenticationSettings.ApplicationParameters.Authority);
            }
        }

        [InlineData(@"blazorwasm\blazorwasm-b2c-hosted", "dotnet new blazorwasm --auth IndividualB2C --aad-b2c-instance https://fabrikamb2c.b2clogin.com --api-client-id fdb91ff5-5ce6-41f3-bdbd-8267c817015d --domain fabrikamb2c.onmicrosoft.com --hosted", "dotnet-blazorwasm-hosted")]
        //[InlineData(@"blazorwasm\blazorwasm-singleorg-callsgraph-hosted", "dotnet new blazorwasm --auth SingleOrg --api-client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --calls-graph --hosted", "dotnet-blazorwasm-hosted")]
        //[InlineData(@"blazorwasm\blazorwasm-singleorg-callswebapi-hosted", "dotnet new blazorwasm --auth SingleOrg --api-client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\" --hosted", "dotnet-blazorwasm-hosted")]
        [InlineData(@"blazorwasm\blazorwasm-singleorg-hosted", "dotnet new blazorwasm --auth SingleOrg --api-client-id 86699d80-dd21-476a-bcd1-7c1a3d471f75 --domain msidentitysamplestesting.onmicrosoft.com  --hosted", "dotnet-blazorwasm-hosted")]
        [Theory(Skip="Test gets stuck on macOS and Linux. Tracking https://github.com/dotnet/Scaffolding/issues/1598 for fix.")]
        public void TestProjectDescriptionReader_TemplatesWithBlazorWasmHosted(string folderPath, string command, string expectedProjectType)
        {
            string createdProjectFolder = CreateProjectIfNeeded(folderPath, command, "ProjectDescriptionReaderTests");

            var projectDescription = _projectDescriptionReader.GetProjectDescription(string.Empty, createdProjectFolder);

            Assert.NotNull(projectDescription);
            Assert.Equal(expectedProjectType, projectDescription.Identifier);

            var authenticationSettings = _codeReader.ReadFromFiles(
                createdProjectFolder,
                projectDescription,
                _projectDescriptionReader.ProjectDescriptions);

            // Blazorwasm now delegates twice (once to the Client [Blazor], and once to the
            // Server [Web API]
            Assert.True(authenticationSettings.ApplicationParameters.IsBlazorWasm);
            Assert.True(authenticationSettings.ApplicationParameters.IsWebApi);
        }

        [InlineData(@"blazorserver\blazorserver-noauth", "dotnet new blazorserver", "dotnet-webapp")]
        [InlineData(@"blazorwasm2\blazorwasm2-noauth", "dotnet new blazorwasm", "dotnet-blazorwasm")]
        [InlineData(@"mvc\mvc-noauth", "dotnet new mvc", "dotnet-webapp")]
        [InlineData(@"webapi\webapi-noauth", "dotnet new webapi", "dotnet-webapi")]
        [InlineData(@"webapp\webapp-noauth", "dotnet new webapp", "dotnet-webapp")]
        [Theory(Skip = "The newly created test project wants new packages that don't exist on official feeds. Will rework for next update.")]
        public void TestProjectDescriptionReader_TemplatesWithNoAuth(string folderPath, string command, string expectedProjectType)
        {
            string createdProjectFolder = CreateProjectIfNeeded(folderPath, command, "NoAuthTests");

            var projectDescription = _projectDescriptionReader.GetProjectDescription(string.Empty, createdProjectFolder);
            Assert.NotNull(projectDescription);
            Assert.Equal(expectedProjectType, projectDescription.Identifier);

            var authenticationSettings = _codeReader.ReadFromFiles(
                createdProjectFolder,
                projectDescription,
                _projectDescriptionReader.ProjectDescriptions);

            Assert.False(authenticationSettings.ApplicationParameters.HasAuthentication);
            Assert.Empty(authenticationSettings.ApplicationParameters.ApiPermissions);
            Assert.Null(authenticationSettings.ApplicationParameters.Authority);
            Assert.Null(authenticationSettings.ApplicationParameters.CalledApiScopes);
            Assert.False(authenticationSettings.ApplicationParameters.CallsDownstreamApi);
            Assert.False(authenticationSettings.ApplicationParameters.CallsMicrosoftGraph);
            Assert.Null(authenticationSettings.ApplicationParameters.ClientId);
            Assert.Null(authenticationSettings.ApplicationParameters.Domain);
            Assert.Null(authenticationSettings.ApplicationParameters.Domain1);
            Assert.Null(authenticationSettings.ApplicationParameters.Instance);
            Assert.False(authenticationSettings.ApplicationParameters.IsAAD);
            Assert.False(authenticationSettings.ApplicationParameters.IsB2C);
            Assert.Null(authenticationSettings.ApplicationParameters.TenantId);
        }

        private void AssertAuthSettings(ProjectAuthenticationSettings authenticationSettings, bool isB2C = false)
        {
            bool IsWebApi = authenticationSettings.ApplicationParameters.IsWebApi.HasValue && authenticationSettings.ApplicationParameters.IsWebApi.Value;
            bool IsWebApp = authenticationSettings.ApplicationParameters.IsWebApp.HasValue && authenticationSettings.ApplicationParameters.IsWebApp.Value;
            bool IsBlazorWasm = authenticationSettings.ApplicationParameters.IsBlazorWasm;
            Assert.True(IsWebApi || IsWebApp || IsBlazorWasm);
            
            if (isB2C)
            {
                Assert.True(authenticationSettings.ApplicationParameters.HasAuthentication);
                Assert.True(authenticationSettings.ApplicationParameters.IsB2C);
                Assert.Equal(TestConstants.B2CClientId, authenticationSettings.ApplicationParameters.ClientId);
                Assert.Equal(TestConstants.B2CDomain, authenticationSettings.ApplicationParameters.Domain);
                Assert.Equal(TestConstants.B2CDomain1, authenticationSettings.ApplicationParameters.Domain1);
            }
            else
            {
                Assert.True(authenticationSettings.ApplicationParameters.HasAuthentication);
                Assert.True(authenticationSettings.ApplicationParameters.IsAAD);
                Assert.Null(authenticationSettings.ApplicationParameters.Instance);
                Assert.Equal(TestConstants.ClientId, authenticationSettings.ApplicationParameters.ClientId);
                Assert.Equal(TestConstants.Domain, authenticationSettings.ApplicationParameters.Domain);
                Assert.Equal(TestConstants.Domain1, authenticationSettings.ApplicationParameters.Domain1);
            }
        }

        /// <summary>
        /// Creates a project from the project templates if the folder does not already exists
        /// </summary>
        /// <param name="projectFolderName">Name of the folder containing the project</param>
        /// <param name="command">dotnet new command to execute to create the code</param>
        /// <param name="testName">Name of the test (parent folder name)</param>
        /// <returns>Name of the folder </returns>
        private string CreateProjectIfNeeded(string projectFolderName, string command, string testName)
        {

            string tempFolder = Environment.GetEnvironmentVariable("Agent.TempDirectory")
                ?? ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? "C:\\temp" : "");

            // Create the folder
            string parentFolder = Path.Combine(tempFolder, "Provisioning", testName);
            string createdProjectFolder = Path.Combine(
                parentFolder, 
                projectFolderName.Replace("\\", Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)));

            if (!Directory.Exists(createdProjectFolder))
            {
                _testOutput.WriteLine($"Creating folder {createdProjectFolder}");
                // dotnet new command to create the project
                TestUtilities.RunProcess(_testOutput, command, createdProjectFolder, " --force");

                // Add the capability of holding user secrets aside of appsettings.json if needed
                if (command.Contains("--calls"))
                {
                    try
                    {
                        TestUtilities.RunProcess(_testOutput, "dotnet user-secrets init", createdProjectFolder);
                    }
                    catch
                    {
                        // Silent catch
                    }
                }
            }

            return createdProjectFolder;
        }
    }
}

