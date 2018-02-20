using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public class IdentityGeneratorCommandLineModel
    {
        [Option(Name = "rootNamespace", ShortName = "rn", Description = "Root namesapce to use for generating identity code." )]
        public string RootNamespace { get; set; }

        [Option(Name = "useSqLite", ShortName ="sqlite", Description = "Flag to specify if DbContext should use SQLite instead of SQL Server.")]
        public bool UseSQLite { get; set; }

        [Option(Name = "dbContext", ShortName = "dc", Description = "Name of the DbContext class to generate.")]
        public string DbContext { get; set; }

        [Option(Name = "userClass", ShortName = "u", Description = "Name of the User class to generate.")]
        public string UserClass { get; set; }

        [Option(Name = "files", ShortName = "fi", Description = "List of semicolon separated files to scaffold. Use the --list-files option to see the available options.")]
        public string Files { get; set; }

        [Option(Name = "listFiles", Description = "Lists the files that can be scaffolded by using the '--files' option.")]
        public bool ListFiles { get; set; }

        [Option(Name = "force", ShortName = "f", Description = "Use this option to overwrite existing files.")]
        public bool Force { get; set; }

        public bool IsGenerateCustomUser
        {
            get
            {
                return !string.IsNullOrEmpty(UserClass) && !string.IsNullOrEmpty(DbContext);
            }
        }
    }
}