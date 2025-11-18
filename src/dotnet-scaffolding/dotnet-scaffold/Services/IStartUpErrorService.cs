namespace Microsoft.DotNet.Tools.Scaffold.Services
{
    /// <summary>
    /// Interface for StartUpErrorService to store and retrieve Azure CLI startup errors.
    /// </summary>
    public interface IStartUpErrorService
    {
        void SetError(string? error);
        string? GetError();
    }
}
