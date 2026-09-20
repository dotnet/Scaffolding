// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class IdentityCodeModificationStep(
    ILogger<IdentityCodeModificationStep> logger,
    ITelemetryService telemetryService) : WrappedCodeModificationStep(logger, telemetryService)
{
    public required ICodeService CodeService { get; set; }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var document = await CodeService.GetDocumentAsync("Program.cs");
        var root = document is null ? null : await document.GetSyntaxRootAsync(cancellationToken);
        var config = JsonNode.Parse(File.ReadAllText(CodeModifierConfigPath!));
        if (root is null || config?["Files"]?[0]?["Methods"]?["Global"]?["CodeChanges"] is not JsonArray changes)
        {
            logger.LogError("Unable to read Program.cs or the Identity code modification configuration.");
            return false;
        }

        var registration = IdentityHelper.FindIdentityRegistration(root);
        if (registration is null)
        {
            CodeChangeOptions.Add("AddDefaultIdentity");
        }
        else
        {
            CodeChangeOptions.Remove("AddDefaultIdentity");
            foreach (var change in GetMissingIdentityChanges(root, registration))
            {
                changes.Add(JsonSerializer.SerializeToNode(change));
            }
        }

        CodeModifierConfigJsonText = config.ToJsonString();
        return await base.ExecuteAsync(context, cancellationToken);
    }

    internal static IEnumerable<object> GetMissingIdentityChanges(SyntaxNode root, InvocationExpressionSyntax registration)
    {
        var calls = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
        var names = calls.Select(GetMethodName).ToHashSet(StringComparer.Ordinal);
        var parent = registration.ToString();

        if (!names.Contains("AddEntityFrameworkStores") && !names.Contains("AddUserStore"))
        {
            yield return new
            {
                Parent = parent,
                Block = "AddEntityFrameworkStores<$(DbContextName)>()",
                CodeChangeType = "MemberAccess"
            };
        }

        if (GetMethodName(registration) == "AddDefaultIdentity")
        {
            yield break;
        }

        if (!names.Contains("AddDefaultUI"))
        {
            yield return new { Parent = parent, Block = "AddDefaultUI()", CodeChangeType = "MemberAccess" };
        }
        if (!names.Contains("AddDefaultTokenProviders") && !names.Contains("AddTokenProvider"))
        {
            yield return new { Parent = parent, Block = "AddDefaultTokenProviders()", CodeChangeType = "MemberAccess" };
        }

        if (GetMethodName(registration) != "AddIdentityCore" || names.Contains("AddIdentityCookies"))
        {
            yield break;
        }

        var services = ((MemberAccessExpressionSyntax)registration.Expression).Expression.ToString();
        yield return new
        {
            InsertBefore = new[] { "builder.Build()", "WebApplication.CreateBuilder.Build()" },
            Block = $$"""
{{services}}.AddAuthentication(options =>
{
    options.DefaultScheme ??= IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme ??= IdentityConstants.ExternalScheme;
})
""",
        };

        foreach (var (method, scheme) in new[]
        {
            ("AddApplicationCookie", "Application"),
            ("AddExternalCookie", "External"),
            ("AddTwoFactorRememberMeCookie", "TwoFactorRememberMe"),
            ("AddTwoFactorUserIdCookie", "TwoFactorUserId")
        })
        {
            var hasCookie = names.Contains(method) || calls.Any(call =>
                GetMethodName(call) == "AddCookie" &&
                call.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString() is { } argument &&
                (argument == $"IdentityConstants.{scheme}Scheme" || argument == $"\"Identity.{scheme}\""));
            if (!hasCookie)
            {
                yield return new
                {
                    InsertBefore = new[] { "builder.Build()", "WebApplication.CreateBuilder.Build()" },
                    Block = $"{services}.AddAuthentication().{method}()"
                };
            }
        }
    }

    private static string? GetMethodName(InvocationExpressionSyntax call)
        => call.Expression is MemberAccessExpressionSyntax member ? member.Name.Identifier.ValueText : null;
}
