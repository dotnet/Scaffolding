// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

internal class Constants
{
    public const string BlazorExtension = ".razor";
    public const string ViewExtension = ".cshtml";
    public const string GlobalNamespace = "<global namespace>";

    public class CliOptions
    {
        public const string ProjectCliOption = "--project";
        public const string PrereleaseCliOption = "--prerelease";
        public const string NameOption = "--name";
        //model with ef options
        public const string ModelCliOption = "--model";
        public const string DataContextOption = "--dataContext";
        public const string DbProviderOption = "--dbProvider";
        //crud options
        public const string PageTypeOption = "--page";
        public const string ViewsOption = "--views";
        //minimal api options
        public const string OpenApiOption = "--open";
        public const string EndpointsOption = "--endpoints";
        //controller options
        public const string ActionsOption = "--actions";
        public const string ControllerNameOption = "--controller";
    }
}
