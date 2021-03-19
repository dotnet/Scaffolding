// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Web.CodeGeneration.CommandLine;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    //Command line parameters common to controller and view scaffolder.
    public abstract class CommonCommandLineModel
    {
        [Option(Name = "model", ShortName = "m", Description = "Model class to use")]
        public string ModelClass { get; set; }

        [Option(Name = "dataContext", ShortName = "dc", Description = "DbContext class to use")]
        public string DataContextClass { get; set; }

        [Option(Name = "useSqlite", ShortName ="sqlite", Description = "Flag to specify if DbContext should use SQLite instead of SQL Server.")]
        public bool UseSqlite { get; set; }

        [Option(Name = "referenceScriptLibraries", ShortName = "scripts", Description = "Switch to specify whether to reference script libraries in the generated views")]
        public bool ReferenceScriptLibraries { get; set; }

        [Option(Name = "layout", ShortName = "l", Description = "Custom Layout page to use")]
        public string LayoutPage { get; set; }

        [Option(Name = "useDefaultLayout", ShortName = "udl", Description = "Switch to specify that default layout should be used for the views")]
        public bool UseDefaultLayout { get; set; }

        [Option(Name = "force", ShortName = "f", Description = "Use this option to overwrite existing files")]
        public bool Force { get; set; }

        [Option(Name = "relativeFolderPath", ShortName = "outDir", Description = "Specify the relative output folder path from project where the file needs to be generated, if not specified, file will be generated in the project folder")]
        public string RelativeFolderPath { get; set; }

        [Option(Name = "controllerNamespace", ShortName = "namespace", Description = "Specify the name of the namespace to use for the generated controller")]
        public string ControllerNamespace { get; set; }

        public abstract CommonCommandLineModel Clone();

        protected CommonCommandLineModel()
        {
        }

        protected CommonCommandLineModel(CommonCommandLineModel copyFrom)
        {
            ModelClass = copyFrom.ModelClass;
            DataContextClass = copyFrom.DataContextClass;
            ReferenceScriptLibraries = copyFrom.ReferenceScriptLibraries;
            LayoutPage = copyFrom.LayoutPage;
            UseDefaultLayout = copyFrom.UseDefaultLayout;
            Force = copyFrom.Force;
            RelativeFolderPath = copyFrom.RelativeFolderPath;
            ControllerNamespace = copyFrom.ControllerNamespace;
            UseSqlite = copyFrom.UseSqlite;
        }
    }
}