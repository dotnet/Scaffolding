namespace Microsoft.DotNet.Scaffolding.Internal.Services;

public interface IEnvironmentService
{
    string UserProfilePath { get; }
    string NugetCachePath { get; }
    string LocalUserFolderPath { get; }
    string DotnetUserProfilePath { get; }
    string CurrentDirectory { get; }
    bool Is64BitOperatingSystem { get; }
    bool Is64BitProcess { get; }
    string? DomainName { get; }
    OperatingSystem OS { get; }
    string GetMachineName();
    string? GetEnvironmentVariable(string name);
    bool GetEnvironmentVariableAsBool(string name, bool defaultValue = false);
    void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget envTarget = EnvironmentVariableTarget.Process);
    string GetFolderPath(System.Environment.SpecialFolder specifalFolder);
    string ExpandEnvironmentVariables(string name);
}
