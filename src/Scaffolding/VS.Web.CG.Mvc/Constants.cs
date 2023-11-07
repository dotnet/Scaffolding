// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public const string BlazorExtension = ".razor";

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
