// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;


namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public class ApplicationEnvironment : IApplicationEnvironment
    {
        public ApplicationEnvironment(string appName, string appBasePath)
            : this(appName, appBasePath, "Debug")
        {

        }
        
        public ApplicationEnvironment(string appName, string appBasePath, string appConfiguration) 
        {
            if(appName == null)
            {
                throw new ArgumentNullException(nameof(appName));
            }
            if(appBasePath == null)
            {
                throw new ArgumentNullException(nameof(appBasePath));
            }
            if(appConfiguration == null) 
            {
                throw new ArgumentNullException(nameof(appConfiguration));
            }
            ApplicationName = appName;
            ApplicationBasePath = appBasePath;
            ApplicationConfiguration = appConfiguration;
        }
        public string ApplicationBasePath
        {
            get; private set;
        }

        public string ApplicationName
        {
            get; private set;
        }
        
        public string ApplicationConfiguration
        {
            get; private set;
        }

    }
}
