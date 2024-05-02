namespace Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;

public interface IEnvironmentService
{
    string DotnetScaffolderFolder { get; }
    string ManifestFile { get; }
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
    string InitializeAndGetManifestFile();
    void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget envTarget = EnvironmentVariableTarget.Process);
    string GetFolderPath(System.Environment.SpecialFolder specifalFolder);
    string ExpandEnvironmentVariables(string name);
}
