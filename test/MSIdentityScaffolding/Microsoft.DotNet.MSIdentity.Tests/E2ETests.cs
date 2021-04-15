// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.DotNet.MSIdentity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class E2ETests
    {
        private readonly ITestOutputHelper testOutput;

        public E2ETests(ITestOutputHelper output)
        {
            this.testOutput = output;
        }

        [InlineData("webapp\\webapp-noauth", "dotnet new webapp")]
        [InlineData("webapp\\webapp-singleorg", "dotnet new webapp --auth SingleOrg")]
        [InlineData("webapp\\webapp-singleorg-callsgraph", "dotnet new webapp --auth SingleOrg --calls-graph")]
        [InlineData("webapp\\webapp-singleorg-callswebapi", "dotnet new webapp --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("webapp\\webapp-b2c", "dotnet new webapp --auth IndividualB2C")]
        [InlineData("webapp\\webapp-b2c-callswebapi", "dotnet new webapp --auth IndividualB2C --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"")]
        [InlineData("webapi\\webapi-noauth", "dotnet new webapi")]
        [InlineData("webapi\\webapi-singleorg", "dotnet new webapi --auth SingleOrg")]
        [InlineData("webapi\\webapi-singleorg-callsgraph", "dotnet new webapi --auth SingleOrg --calls-graph")]
        [InlineData("webapi\\webapi-singleorg-callswebapi", "dotnet new webapi --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("webapi\\webapi-b2c", "dotnet new webapi --auth IndividualB2C")]
        [InlineData("mvc\\mvc-noauth", "dotnet new mvc")]
        [InlineData("mvc\\mvc-singleorg", "dotnet new mvc --auth SingleOrg")]
        [InlineData("mvc\\mvc-singleorg-callsgraph", "dotnet new mvc --auth SingleOrg --calls-graph")]
        [InlineData("mvc\\mvc-singleorg-callswebapi", "dotnet new mvc --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("mvc\\mvc-b2c", "dotnet new mvc --auth IndividualB2C")]
        [InlineData("mvc\\mvc-b2c-callswebapi", "dotnet new mvc --auth IndividualB2C --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"")]
        [InlineData("blazorserver\\blazorserver-noauth", "dotnet new blazorserver")]
        [InlineData("blazorserver\\blazorserver-singleorg", "dotnet new blazorserver --auth SingleOrg")]
        [InlineData("blazorserver\\blazorserver-singleorg-callsgraph", "dotnet new blazorserver --auth SingleOrg --calls-graph")]
        [InlineData("blazorserver\\blazorserver-singleorg-callswebapi", "dotnet new blazorserver --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("blazorserver\\blazorserver-b2c", "dotnet new blazorserver --auth IndividualB2C")]
        [InlineData("blazorserver\\blazorserver-b2c-callswebapi", "dotnet new blazorserver --auth IndividualB2C --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"")]
        [InlineData("blazorwasm\\blazorwasm-noauth", "dotnet new blazorwasm")]
        [InlineData("blazorwasm\\blazorwasm-singleorg", "dotnet new blazorwasm --auth SingleOrg")]
        [InlineData("blazorwasm\\blazorwasm-singleorg-callsgraph", "dotnet new blazorwasm --auth SingleOrg --calls-graph")]
        [InlineData("blazorwasm\\blazorwasm-singleorg-callswebapi", "dotnet new blazorwasm --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("blazorwasm\\blazorwasm-singleorg-hosted", "dotnet new blazorwasm --auth SingleOrg  --hosted")]
        [InlineData("blazorwasm\\blazorwasm-singleorg-callsgraph-hosted", "dotnet new blazorwasm --auth SingleOrg --calls-graph --hosted")]
        [InlineData("blazorwasm\\blazorwasm-singleorg-callswebapi-hosted", "dotnet new blazorwasm --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\" --hosted")]
        [InlineData("blazorwasm\\blazorwasm-b2c", "dotnet new blazorwasm --auth IndividualB2C")]
        [InlineData("blazorwasm\\blazorwasm2-b2c-hosted", "dotnet new blazorwasm --auth IndividualB2C  --hosted")]
        [Theory]
        public async Task TestNewAppEndToEnd(string folder, string command)
        {
            // Create the folder
            string executionFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            executionFolder = "C:\\temp\\test-provisionning";
            string folderToCreate = Path.Combine(executionFolder, "Tests", folder);

            // dotnet new command to create the project
            RunProcess(command, folderToCreate, " --force");

            // Add a secret if needed
            if (command.Contains("--calls"))
            {
                try
                {
                    RunProcess("dotnet user-secrets init", folderToCreate);
                }
                catch
                {
                    // Silent catch
                }
            }

            string currentDirectory = Directory.GetCurrentDirectory();

            // Run the tool
            try
            {
                Directory.SetCurrentDirectory(folderToCreate);
                string tenantId = folder.Contains("b2c") ?
                    "fabrikamb2c.onmicrosoft.com" :
                    "testprovisionningtool.onmicrosoft.com";
                await Program.Main(tenantId: tenantId, username: null, clientId: null,
                    folder: null, clientSecret: null, unregister: null, susiPolicyId: null, apiClientId:null, appIdUri:null);
            }
            catch (Exception ex)
            {
                testOutput.WriteLine(ex.ToString());
                Assert.True(false);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }

            testOutput.WriteLine($"{folderToCreate}");
        }

        /// <summary>
        /// Create the test project
        /// </summary>
        /// <param name="command"></param>
        /// <param name="folderToCreate"></param>
        private void RunProcess(string command, string folderToCreate, string postFix="")
        {
            Directory.CreateDirectory(folderToCreate);
            ProcessStartInfo processStartInfo = new ProcessStartInfo("dotnet", command.Replace("dotnet ", string.Empty) + postFix);
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            Environment.GetEnvironmentVariables();
            processStartInfo.WorkingDirectory = folderToCreate;
            Process process = Process.Start(processStartInfo);
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            testOutput.WriteLine(output);
            string errors = process.StandardError.ReadToEnd();
            testOutput.WriteLine(errors);
            Assert.Equal(string.Empty, errors);
        }

        //[InlineData("webapp\\webapp-noauth", "dotnet new webapp")]
        //[InlineData("webapp\\webapp-singleorg", "dotnet new webapp --auth SingleOrg")]
        // [InlineData("webapp\\webapp-singleorg-callsgraph", "dotnet new webapp --auth SingleOrg --calls-graph")]
        //[InlineData("webapp\\webapp-singleorg-callswebapi", "dotnet new webapp --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        //[InlineData("webapp\\webapp-b2c", "dotnet new webapp --auth IndividualB2C")]
        //[InlineData("webapp\\webapp-b2c-callswebapi", "dotnet new webapp --auth IndividualB2C --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"")]
        //[InlineData("webapi\\webapi-noauth", "dotnet new webapi")]
        //[InlineData("webapi\\webapi-singleorg", "dotnet new webapi --auth SingleOrg")]
        //[InlineData("webapi\\webapi-singleorg-callsgraph", "dotnet new webapi --auth SingleOrg --calls-graph")]
        //[InlineData("webapi\\webapi-singleorg-callswebapi", "dotnet new webapi --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        //[InlineData("webapi\\webapi-b2c", "dotnet new webapi --auth IndividualB2C")]
        //[InlineData("webapi\\webapi2-b2c-callswebapi", "dotnet new webapi --auth IndividualB2C --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"")]
        //[InlineData("mvc\\mvc-noauth", "dotnet new mvc")]
        //[InlineData("mvc\\mvc-singleorg", "dotnet new mvc --auth SingleOrg")]
        //[InlineData("mvc\\mvc-singleorg-callsgraph", "dotnet new mvc --auth SingleOrg --calls-graph")]
        //[InlineData("mvc\\mvc-singleorg-callswebapi", "dotnet new mvc --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        //[InlineData("mvc\\mvc-b2c", "dotnet new mvc --auth IndividualB2C")]
        //[InlineData("mvc\\mvc-b2c-callswebapi", "dotnet new mvc --auth IndividualB2C --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"")]
        //[InlineData("blazorserver\\blazorserver-noauth", "dotnet new blazorserver")]
        //[InlineData("blazorserver\\blazorserver-singleorg", "dotnet new blazorserver --auth SingleOrg")]
        //[InlineData("blazorserver\\blazorserver-singleorg-callsgraph", "dotnet new blazorserver --auth SingleOrg --calls-graph")]
        //[InlineData("blazorserver\\blazorserver-singleorg-callswebapi", "dotnet new blazorserver --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        //[InlineData("blazorserver\\blazorserver-b2c", "dotnet new blazorserver --auth IndividualB2C")]
        //[InlineData("blazorserver\\blazorserver-b2c-callswebapi", "dotnet new blazorserver --auth IndividualB2C --called-api-url \"https://fabrikamb2chello.azurewebsites.net/hello\" --called-api-scopes \"https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read\"")]
        //[InlineData("blazorwasm\\blazorwasm-noauth", "dotnet new blazorwasm")]
        //[InlineData("blazorwasm\\blazorwasm-singleorg", "dotnet new blazorwasm --auth SingleOrg")]
        //[InlineData("blazorwasm\\blazorwasm-singleorg-callsgraph", "dotnet new blazorwasm --auth SingleOrg --calls-graph")]
        //[InlineData("blazorwasm\\blazorwasm-singleorg-callswebapi", "dotnet new blazorwasm --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\"")]
        [InlineData("blazorwasm\\blazorwasm-singleorg-hosted", "dotnet new blazorwasm --auth SingleOrg  --hosted")]
        //[InlineData("blazorwasm\\blazorwasm-singleorg-callsgraph-hosted", "dotnet new blazorwasm --auth SingleOrg --calls-graph --hosted")]
        //[InlineData("blazorwasm\\blazorwasm-singleorg-callswebapi-hosted", "dotnet new blazorwasm --auth SingleOrg --called-api-url \"https://graph.microsoft.com/beta/me\" --called-api-scopes \"user.read\" --hosted")]
        //[InlineData("blazorwasm\\blazorwasm-b2c", "dotnet new blazorwasm --auth IndividualB2C")]
        //[InlineData("blazorwasm2\\blazorwasm2-b2c-hosted", "dotnet new blazorwasm --auth IndividualB2C  --hosted")]
        [Theory]
        public async Task TestUpdateAppEndToEnd(string folder, string command)
        {
            // Create the folder
            string executionFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            executionFolder = "C:\\temp\\test-provisionning";
            string folderToCreate = Path.Combine(executionFolder, "Tests", folder);

            // dotnet new command to create the project
            RunProcess(command, folderToCreate, " --force");

            // Add a secret if needed
            if (command.Contains("--calls"))
            {
                try
                {
                    RunProcess("dotnet user-secrets init", folderToCreate);
                }
                catch
                {
                    // Silent catch
                }
            }

            string currentDirectory = Directory.GetCurrentDirectory();

            // Run the tool
            try
            {
                Directory.SetCurrentDirectory(folderToCreate);

                Directory.SetCurrentDirectory(folderToCreate);
                string tenantId = folder.Contains("b2c") ?
                    "fabrikamb2c.onmicrosoft.com" :
                    "testprovisionningtool.onmicrosoft.com";
                await Program.Main(tenantId: tenantId, username: null, clientId: null,
                    folder: null, clientSecret: null, unregister: null, susiPolicyId: null, apiClientId: null, appIdUri: null);
            }
            catch (Exception ex)
            {
                testOutput.WriteLine(ex.ToString());
                Assert.True(false);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }

            testOutput.WriteLine($"{folderToCreate}");
        }
    }
}
