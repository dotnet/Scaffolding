using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.Scaffold;

public class ScaffoldCommandApp
{
    private readonly string[] _args;
    private readonly Spectre.Console.Cli.CommandApp<ScaffoldCommand> _commandApp;

    public ScaffoldCommandApp(Spectre.Console.Cli.CommandApp<ScaffoldCommand> commandApp, string[] args)
    {
        _commandApp = commandApp;
        _args = args;
    }

    public Task<int> RunAsync()
    {
        return _commandApp.RunAsync(_args);
    }
}

