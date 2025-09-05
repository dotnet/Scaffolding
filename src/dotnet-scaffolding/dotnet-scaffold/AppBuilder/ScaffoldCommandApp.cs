using Microsoft.DotNet.Tools.Scaffold.Command;

namespace Microsoft.DotNet.Tools.Scaffold;

/// <summary>
/// Represents the application entry point for running the scaffold command.
/// This class wraps the Spectre.Console.Cli.CommandApp for the <see cref="ScaffoldCommand"/>.
/// </summary>
internal class ScaffoldCommandApp
{
    // Command-line arguments passed to the application.
    private readonly string[] _args;

    // The Spectre.Console command application instance for the scaffold command.
    private readonly Spectre.Console.Cli.CommandApp<ScaffoldCommand> _commandApp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScaffoldCommandApp"/> class.
    /// </summary>
    /// <param name="commandApp">The command application to execute.</param>
    /// <param name="args">The command-line arguments.</param>
    public ScaffoldCommandApp(Spectre.Console.Cli.CommandApp<ScaffoldCommand> commandApp, string[] args)
    {
        _commandApp = commandApp;
        _args = args;
    }

    /// <summary>
    /// Runs the scaffold command application asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, with the process exit code as result.</returns>
    public Task<int> RunAsync()
    {
        return _commandApp.RunAsync(_args);
    }
}

