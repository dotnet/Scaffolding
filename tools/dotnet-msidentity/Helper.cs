using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.Extensions.Internal;


namespace Microsoft.DotNet.MSIdentity
{
    internal class Helper
    {
        public static void AddPackage()
        {
            string packageName = "Microsoft.DotNet.MSIdentity";
            string packageVersion = "1.0.0";
            string tfm = "net6.0";
            var errors = new List<string>();
            var output = new List<string>();
            var arguments = new List<string>();
            arguments.Add("package");
            arguments.Add(packageName);
            if (!string.IsNullOrEmpty(packageVersion))
            {
                arguments.Add("-v");
                arguments.Add(packageVersion);
            }

            if (tfm.Equals("net7.0"))
            {
                arguments.Add("--prerelease");
            }

            if (!string.IsNullOrEmpty(tfm))
            {
                arguments.Add("-f");
                arguments.Add(tfm);
            }
            IConsoleLogger consoleLogger = new ConsoleLogger();
            consoleLogger.LogMessage(string.Format("Adding package {0} . . .", packageName));

            var result = Command.CreateDotNet(
                "add",
                arguments.ToArray())
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

            if (result.ExitCode != 0)
            {
                consoleLogger.LogMessage(string.Format("Failed to add package {0}", packageName));
            }
            else
            {
                consoleLogger.LogMessage("SUCCESS\n");
            }

            errors = new List<string>();
            output = new List<string>();
            arguments = new List<string>();
            arguments.Add("testingnewtool.csproj");
            arguments.Add("-t:Rebuild");
            result = Command.CreateDotNet(
                "msbuild",
                arguments.ToArray())
                .OnErrorLine(e => errors.Add(e))
                .OnOutputLine(o => output.Add(o))
                .Execute();

            if (result.ExitCode != 0)
            {
                consoleLogger.LogMessage("Failed to restore nuget packages\n");
            }
            else
            {
                consoleLogger.LogMessage("Restored nuget packages successfully\n");
            }
        }
    }
}
