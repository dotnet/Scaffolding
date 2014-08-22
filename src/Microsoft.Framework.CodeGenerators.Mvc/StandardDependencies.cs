// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    internal static class StandardDependencies
    {
        private static StartupContent _staticFilesStartUpContent;
        private static StartupContent _mvcStartUpContent;

        private static Dependency _staticFilesDependency;
        private static Dependency _mvcDependency;

        public static Dependency StaticFilesDependency
        {
            get
            {
                if (_staticFilesDependency == null)
                {
                    _staticFilesDependency = new Dependency()
                    {
                        Name = PackageConstants.StaticFilesPackage,
                        Version = PackageConstants.StaticFilesVersion,
                        StartupConfiguration = StaticFilesStartupContent
                    };
                }
                return _staticFilesDependency;
            }
        }

        public static StartupContent StaticFilesStartupContent
        {
            get
            {
                if (_staticFilesStartUpContent == null)
                {
                    _staticFilesStartUpContent = new StartupContent();
                    _staticFilesStartUpContent.AddRequiredNamespace(PackageConstants.StaticFilesNamespace);
                    _staticFilesStartUpContent.AddUseStatement("// Add static files to the request pipeline"); //Todo: Resources?
                    _staticFilesStartUpContent.AddUseStatement("app.UseStaticFiles();");
                }
                return _staticFilesStartUpContent;
            }
        }

        public static Dependency MvcDependency
        {
            get
            {
                if (_mvcDependency == null)
                {
                    _mvcDependency = new Dependency()
                    {
                        Name = PackageConstants.MvcPackage,
                        Version = PackageConstants.MvcVersion,
                        StartupConfiguration = MvcStartupContent
                    };
                }
                return _mvcDependency;
            }
        }

        public static StartupContent MvcStartupContent
        {
            get
            {
                if (_mvcStartUpContent == null)
                {
                    _mvcStartUpContent = new StartupContent();

                    _mvcStartUpContent.AddRequiredNamespace(PackageConstants.BuilderNamespace);
                    _mvcStartUpContent.AddRequiredNamespace(PackageConstants.DependencyInjectionNamespace);

                    _mvcStartUpContent.AddServiceStatement("// Add MVC services to the services container"); //Todo: Resources?
                    _mvcStartUpContent.AddServiceStatement("services.AddMvc();");

                    _mvcStartUpContent.AddUseStatement("// Add MVC to the request pipeline"); //Todo: Resources?
                    _mvcStartUpContent.AddUseStatement(@"app.UseMvc(routes =>");
                    _mvcStartUpContent.AddUseStatement(@"{");
                    _mvcStartUpContent.AddUseStatement(@"    routes.MapRoute(");
                    _mvcStartUpContent.AddUseStatement(@"        name: ""default"",");
                    _mvcStartUpContent.AddUseStatement(@"        template: ""{controller}/{action}/{id?}"",");
                    _mvcStartUpContent.AddUseStatement(@"        defaults: new { controller = ""Home"", action = ""Index"" });");
                    _mvcStartUpContent.AddUseStatement("");
                    _mvcStartUpContent.AddUseStatement(@"    routes.MapRoute(");
                    _mvcStartUpContent.AddUseStatement(@"        name: ""api"",");
                    _mvcStartUpContent.AddUseStatement(@"        template: ""{controller}/{id?}"");");
                    _mvcStartUpContent.AddUseStatement(@"});");
                }
                return _mvcStartUpContent;
            }
        }
    }
}