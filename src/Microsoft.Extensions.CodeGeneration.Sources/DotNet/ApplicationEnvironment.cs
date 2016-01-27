using System;
using System.Collections.Generic;


namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public class ApplicationEnvironment : IApplicationEnvironment
    {
        public ApplicationEnvironment(string appName, string appBasePath)
        {
            if(appName == null)
            {
                throw new ArgumentNullException(nameof(appName));
            }
            if(appBasePath == null)
            {
                throw new ArgumentNullException(nameof(appBasePath));
            }

            ApplicationName = appName;
            ApplicationBasePath = appBasePath;
        }

        public string ApplicationBasePath
        {
            get; private set;
        }

        public string ApplicationName
        {
            get; private set;
        }

    }
}
