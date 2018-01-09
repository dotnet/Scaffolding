using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity
{
    public class IdentityGeneratorCommandLineModel
    {
        [Option(Name = "rootNamespace", ShortName = "rn", Description = "Root namesapce to use for generating identity code." )]
        public string RootNamespace { get; set; }

        [Option(Name = "skipLayoutPage", ShortName ="slp", Description = "Flag to specify that the Layout page should not be generated.")]
        public bool SkipLayoutPage { get; set; }

        [Option(Name = "dbContext", ShortName = "dc", Description = "Name of the DbContext class to generate.")]
        public string DbContext { get; set; }

        [Option(Name = "userClass", ShortName = "u", Description = "Name of the User class to generate.")]
        public string UserClass { get; set; }

        [Option(Name = "force", ShortName = "f", Description = "Use this option to overwrite existing files.")]
        public bool Force { get; set; }
    }
}