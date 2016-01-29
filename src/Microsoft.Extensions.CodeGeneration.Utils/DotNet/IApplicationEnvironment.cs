using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public interface IApplicationEnvironment
    {
        string ApplicationBasePath { get; }
        string ApplicationName { get; }
    }
}
