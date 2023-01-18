using System;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public class IdentityGeneratorCommandLineModel
    {
        [Option(Name = "rootNamespace", ShortName = "rn", Description = "Root namesapce to use for generating identity code.")]
        public string RootNamespace { get; set; }

        [Obsolete("Use --databaseProvider or -dbProvider to configure database type instead")]
        [Option(Name = "useSqLite", ShortName = "sqlite", Description = "Flag to specify if DbContext should use SQLite instead of SQL Server.")]
        public bool UseSqlite { get; set; }

        [Obsolete("Use --databaseProvider or -dbProvider to configure database type instead")]
        [Option(Name = "useSqlite", ShortName = "sqlite", Description = "Flag to specify if DbContext should use SQLite instead of SQL Server.")]
        public bool UseSqlite2 { get; set; }

        [Option(Name = "databaseProvider", ShortName = "dbProvider", Description = "Database provider to use. Options include 'sqlserver' (default), 'sqlite', 'cosmos', 'postgres'.")]
        public string DatabaseProviderString { get; set; }
        public DbProvider DatabaseProvider { get; set; }

        [Option(Name = "dbContext", ShortName = "dc", Description = "Name of the DbContext to use, or generate (if it does not exist).")]
        public string DbContext { get; set; }

        [Option(Name = "userClass", ShortName = "u", Description = "Name of the User class to generate.")]
        public string UserClass { get; set; }

        [Option(Name = "files", ShortName = "fi", Description = "List of semicolon separated files to scaffold. Use the --listFiles option to see the available options.")]
        public string Files { get; set; }

        [Option(Name = "listFiles", ShortName = "lf", Description = "Lists the files that can be scaffolded by using the '--files' option.")]
        public bool ListFiles { get; set; }

        [Option(Name = "force", ShortName = "f", Description = "Use this option to overwrite existing files.")]
        public bool Force { get; set; }

        [Option(Name = "useDefaultUI", ShortName = "udui", Description = "Use this option to setup identity and to use Default UI.")]
        public bool UseDefaultUI { get; set; }

        [Option(Name = "layout", ShortName = "l", Description = "Specify a custom layout file to use.")]
        public string Layout { get; set; }

        [Option(Name = "generateLayout", ShortName = "gl", Description = "Use this option to generate a new _Layout.cshtml")]
        public bool GenerateLayout { get; set; }

        [Option(Name = "bootstrapVersion", ShortName = "b", Description = "Specify the bootstrap version. Valid values: '3', '4', '5'. Default is 5.")]
        public string BootstrapVersion { get; set; }

        [Option(Name = "excludeFiles", ShortName = "exf", Description = "Use this option to overwrite all but list of semicolon separated files.  Use the --listFiles option to see the available options.")]
        public string ExcludeFiles { get; set; }

        public bool IsGenerateCustomUser
        {
            get
            {
                return !string.IsNullOrEmpty(UserClass) && !string.IsNullOrEmpty(DbContext);
            }
        }
    }
}
