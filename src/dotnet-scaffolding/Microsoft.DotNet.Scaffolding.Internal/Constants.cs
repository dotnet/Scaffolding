// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Internal;

internal static class Constants
{
    public static class EnvironmentVariables
    {
        public const string MSBuildExtensionsPath32 = nameof(MSBuildExtensionsPath32);
        public const string MSBuildExtensionsPath = nameof(MSBuildExtensionsPath);
        public const string MSBUILD_EXE_PATH = nameof(MSBUILD_EXE_PATH);
        public const string MSBuildSDKsPath = nameof(MSBuildSDKsPath);
        public const string USERPROFILE = nameof(USERPROFILE);
        public const string VSINSTALLDIR = nameof(VSINSTALLDIR);
    }

    public static class StepConstants
    {
        public const string BaseProjectPath = nameof(BaseProjectPath);
        public const string CodeModifierProperties = nameof(CodeModifierProperties);
        public const string DbContextProperties = nameof(DbContextProperties);
        public const string AdditionalCodeModifier = nameof(AdditionalCodeModifier);
    }

    public static class CodeModifierPropertyConstants
    {
        public const string EndpointsMethodName = "$(EndpointsMethodName)";
        public const string ConnectionStringName = "$(ConnectionStringName)";
        public const string AutoGenProjectName = "$(AutoGenProjectName)";
        //DbContext related constants
        public const string UseDbMethod = "$(UseDbMethod)";
        public const string DbContextName = "$(DbContextName)";
        public const string DbContextNamespace = "$(DbContextNamespace)";
        //Identity related constants
        public const string UserClassNamespace = "$(UserClassNamespace)";
        public const string BlazorIdentityNamespace = "$(BlazorIdentityNamespace)";
    }
}
