// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold;

public class ScaffoldCommandAppBuilder(string[] args)
{
    private readonly string[] _args = args;

    public ScaffoldCommandAppBuilder AddSettings()
    {
        //action?.Invoke(_appSettings, _configuration);

        return this;
    }

    public ScaffoldCommandApp Build()
    {
        CommandApp commandApp;

        if (HasHelpArgument())
        {
            //commandApp = new CommandApp();
        }
        else
        {
            //var exportProvider = ConfigureExports();
            //commandApp = new CommandApp(new TypeRegistrar(exportProvider));
        }

        commandApp = new CommandApp();
        return new ScaffoldCommandApp(commandApp, _args);
    }

    private bool HasHelpArgument()
    {
        return _args.Any(x =>
            string.Equals("-h", x, StringComparison.OrdinalIgnoreCase) ||
            string.Equals("--help", x, StringComparison.OrdinalIgnoreCase));
    }
}
