// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.CodeGenerators.Mvc
{
    internal static class Constants
    {
        public const string ControllerSuffix = "Controller";
        public const string ControllersFolderName = "Controllers";
        public const string ViewsFolderName = "Views";
        public const string SharedViewsFolderName = "Shared";

        public const string StartupClassName = "Startup";
        public const string ReadMeOutputFileName = "ScaffoldingReadMe.txt";

        public const string ViewExtension = ".cshtml";
        public const string CodeFileExtension = ".cs";
        public const string RazorTemplateExtension = ".cshtml";

        public const string ThisAssemblyName = "Microsoft.Extensions.CodeGenerators.Mvc";

        //Template names
        public const string ApiEmptyControllerTemplate = "ApiEmptyController.cshtml";
        public const string ApiControllerWithActionsTemplate = "ApiControllerWithActions.cshtml";
        public const string ApiControllerWithContextTemplate = "ApiControllerWithContext.cshtml";
        public const string MvcEmptyControllerTemplate = "EmptyController.cshtml";
        public const string MvcControllerWithActionsTemplate = "ControllerWithActions.cshtml";
        public const string MvcControllerWithContextTemplate = "MvcControllerWithContext.cshtml";
    }
}