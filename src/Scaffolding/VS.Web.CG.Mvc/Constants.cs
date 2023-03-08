// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    internal static class Constants
    {
        public const string MicrosoftEntityFrameworkCorePackageName = "Microsoft.EntityFrameworkCore";

        public const string ControllerSuffix = "Controller";
        public const string ControllersFolderName = "Controllers";
        public const string ViewsFolderName = "Views";
        public const string SharedViewsFolderName = "Shared";

        public const string StartupClassName = "Startup";
        public const string ReadMeOutputFileName = "ScaffoldingReadMe.txt";

        public const string ViewExtension = ".cshtml";
        public const string CodeFileExtension = ".cs";
        public const string RazorTemplateExtension = ".cshtml";

        public static readonly string ThisAssemblyName = typeof(Constants).GetTypeInfo().Assembly.GetName().Name;

        //Template names
        public const string ApiControllerWithContextTemplate = "ApiControllerWithContext.cshtml";
        public const string MvcControllerWithContextTemplate = "MvcControllerWithContext.cshtml";
        public const string MinimalApiTemplate = "MinimalApi.cshtml";
        public const string MinimalApiEfTemplate = "MinimalApiEf.cshtml";
        public const string MinimalApiNoClassTemplate = "MinimalApiNoClass.cshtml";
        public const string MinimalApiEfNoClassTemplate = "MinimalApiEfNoClass.cshtml";
    }
}
