// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;
namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor
{
    public class BlazorIdentityCommandLineModel
    {

        [Option(Name = "rootNamespace", ShortName = "rn", Description = "Root namespace to use for generating identity code.")]
        public string RootNamespace { get; set; }

        [Option(Name = "relativeFolderPath", ShortName = "outDir", Description = "Specify the relative output folder path from project where the file needs to be generated, if not specified, file will be generated in the path based on the root namespace.")]
        public string RelativeFolderPath { get; set; }

        [Option(Name = "databaseProvider", ShortName = "dbProvider", Description = "Database provider to use. Options include 'sqlserver' (default), 'sqlite', 'cosmos', 'postgres'.")]
        public string DatabaseProviderString { get; set; }
        public DbProvider DatabaseProvider { get; set; }

        [Option(Name = "dataContext", ShortName = "dc", Description = "DbContext class to use")]
        public string DataContextClass { get; set; }

        [Option(Name = "userClass", ShortName = "u", Description = "Name of the User class to generate.")]
        public string UserClass { get; set; }

        [Option(Name = "files", ShortName = "fi", Description = "List of semicolon separated files to scaffold. Use the --listFiles option to see the available options.")]
        public string Files { get; set; }

        [Option(Name = "listFiles", ShortName = "lf", Description = "Lists the files that can be scaffolded by using the '--files' option.")]
        public bool ListFiles { get; set; }

        [Option(Name = "force", ShortName = "f", Description = "Use this option to overwrite existing files. Existing files will not be overwritten otherwise.")]
        public bool Force { get; set; }

        [Option(Name = "excludeFiles", ShortName = "exf", Description = "Use this option to overwrite all but list of semicolon separated files.  Use the --listFiles option to see the available options.")]
        public string ExcludeFiles { get; set; }

        public BlazorIdentityCommandLineModel()
        {
        }

        protected BlazorIdentityCommandLineModel(BlazorIdentityCommandLineModel copyFrom)
        {
            UserClass = copyFrom.UserClass;
            DataContextClass = copyFrom.DataContextClass;
            RootNamespace = copyFrom.RootNamespace;
            DatabaseProviderString = copyFrom.DatabaseProviderString;
            DatabaseProvider = copyFrom.DatabaseProvider;
            Files = copyFrom.Files;
            ExcludeFiles = copyFrom.ExcludeFiles;
        }

        public BlazorIdentityCommandLineModel Clone()
        {
            return new BlazorIdentityCommandLineModel(this);
        }
    }
}
