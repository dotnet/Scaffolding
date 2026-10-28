// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class BlazorIdentityHelperTests
    {
        [Fact]
        public void HostChanges_EnableAntiforgeryBeforeIdentityEndpoints()
        {
            using var config = ReadHostChanges();
            var program = config.RootElement.GetProperty("Files").EnumerateArray()
                .Single(file => file.GetProperty("FileName").GetString() == "Program.cs");
            var changes = program.GetProperty("Methods").GetProperty("Global").GetProperty("CodeChanges").EnumerateArray();
            var middleware = Assert.Single(changes, change =>
                change.TryGetProperty("Block", out var block) && block.GetString() == "app.UseAntiforgery()");

            Assert.Contains("app.MapAdditionalIdentityEndpoints();",
                middleware.GetProperty("InsertBefore").EnumerateArray().Select(value => value.GetString()));
        }

        [Fact]
        public void HostChanges_EnableAuthorizationForManageEndpoints()
        {
            using var config = ReadHostChanges();
            var program = config.RootElement.GetProperty("Files").EnumerateArray()
                .Single(file => file.GetProperty("FileName").GetString() == "Program.cs");
            var changes = program.GetProperty("Methods").GetProperty("Global").GetProperty("CodeChanges").EnumerateArray();

            Assert.Contains(changes, change => change.TryGetProperty("Block", out var block) &&
                block.GetString() == "WebApplication.CreateBuilder.Services.AddAuthorization()");
        }

        [Fact]
        public void HostChanges_ParameterizeGlobalRenderMode()
        {
            using var config = ReadHostChanges();
            var app = config.RootElement.GetProperty("Files").EnumerateArray()
                .Single(file => file.GetProperty("FileName").GetString() == "Components\\App.razor");
            var text = app.GetRawText();

            Assert.Contains("<HeadOutlet @rendermode=\\\"$(BlazorRenderMode)\\\" />", text);
            Assert.Contains("<Routes @rendermode=\\\"$(BlazorRenderMode)\\\" />", text);
            Assert.Contains("HttpContext.AcceptsInteractiveRouting() ? $(BlazorRenderMode) : null", text);
        }

        [Theory]
        [InlineData("InteractiveServer")]
        [InlineData("InteractiveWebAssembly")]
        [InlineData("InteractiveAuto")]
        public void ConfigureAppRenderMode_PreservesGlobalModeAndExcludesIdentity(string renderMode)
        {
            var changes = ReadAppChanges();
            BlazorIdentityHelper.ConfigureAppRenderMode(changes,
                $"<HeadOutlet @rendermode=\"{renderMode}\" /><Routes @rendermode=\"{renderMode}\" />");

            Assert.Contains(changes.Replacements, change =>
                change.ReplaceSnippet?.Contains($"<Routes @rendermode=\"{renderMode}\" />") == true &&
                change.Block.Contains("<Routes @rendermode=\"PageRenderMode\" />"));
            Assert.Contains(changes.Replacements, change =>
                change.Block.Contains($"HttpContext.AcceptsInteractiveRouting() ? {renderMode} : null"));
            Assert.DoesNotContain(changes.Replacements, change => change.Block.Contains("$(BlazorRenderMode)"));
        }

        [Theory]
        [InlineData("<HeadOutlet /><Routes />")]
        [InlineData("<HeadOutlet @rendermode=\"PageRenderMode\" /><Routes @rendermode=\"PageRenderMode\" />")]
        public void ConfigureAppRenderMode_DoesNotChangePerPageOrAlreadyConfiguredRouting(string appContent)
        {
            var changes = ReadAppChanges();
            BlazorIdentityHelper.ConfigureAppRenderMode(changes, appContent);

            var script = Assert.Single(changes.Replacements);
            Assert.Contains("PasskeySubmit.razor.js", script.Block);
        }

        private static CodeFile ReadAppChanges()
        {
            using var config = ReadHostChanges();
            var app = config.RootElement.GetProperty("Files").EnumerateArray()
                .Single(file => file.GetProperty("FileName").GetString() == "Components\\App.razor");
            return JsonSerializer.Deserialize<CodeFile>(app.GetRawText());
        }

        private static JsonDocument ReadHostChanges()
        {
            var assembly = typeof(BlazorIdentityGenerator).Assembly;
            var resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith("blazorIdentityChanges.json", StringComparison.Ordinal));
            using var stream = assembly.GetManifestResourceStream(resource);
            return JsonDocument.Parse(stream);
        }

        [SkippableTheory]
        [MemberData(nameof(BlazorIdentityFiles))]
        public void GetFormattedRelativeIdentityFileTest(string fullFileName, string expected)
        {
            Skip.If(!OperatingSystem.IsWindows());
            // Arrange & Act
            string result = BlazorIdentityHelper.GetFormattedRelativeIdentityFile(fullFileName);
            // Assert
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> BlazorIdentityFiles
        {
            get
            {
                return new[]
                {
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\file.tt", "file" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\file.tt", "Pages.file" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\Manage\\file.tt", "Pages.Manage.file" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\Manage\\Passkeys.tt", "Pages.Manage.Passkeys" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\Manage\\RenamePasskey.tt", "Pages.Manage.RenamePasskey" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Shared\\PasskeySubmit.tt", "Shared.PasskeySubmit" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\PasskeyInputModel.tt", "PasskeyInputModel" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\PasskeyOperation.tt", "PasskeyOperation" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Thing\\file.tt", "Thing.file" },
                    new object[] { "C:\\Some\\Path\\Templates\\Thing\\file.tt", "" },
                };
            }
        }
    }
}
