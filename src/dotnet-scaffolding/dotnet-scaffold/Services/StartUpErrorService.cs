using System;

namespace Microsoft.DotNet.Tools.Scaffold.Services
{
    /// <summary>
    /// Service to store and retrieve Azure CLI startup errors.
    /// </summary>
    public class StartUpErrorService : IStartUpErrorService
    {
        private string? _azureCliError;

        public void SetError(string? error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                _azureCliError = error;
            }
        }

        public string? GetError()
        {
            return _azureCliError;
        }
    }
}
