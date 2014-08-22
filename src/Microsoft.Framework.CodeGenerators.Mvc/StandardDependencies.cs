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
                    _staticFilesStartUpContent.UseStatements.Add("// Add static files to the request pipeline"); //Todo: Resources?
                    _staticFilesStartUpContent.UseStatements.Add("app.UseStaticFiles();");
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

                    _mvcStartUpContent.RequiredNamespaces.Add(PackageConstants.RoutingNamespace);
                    _mvcStartUpContent.RequiredNamespaces.Add(PackageConstants.DependencyInjectionNamespace);

                    _mvcStartUpContent.ServiceStatements.Add("// Add MVC services to the services container"); //Todo: Resources?
                    _mvcStartUpContent.ServiceStatements.Add("services.AddMvc();");

                    _mvcStartUpContent.UseStatements.Add("// Add MVC to the request pipeline"); //Todo: Resources?
                    _mvcStartUpContent.UseStatements.Add(@"app.UseMvc(routes =>");
                    _mvcStartUpContent.UseStatements.Add(@"{");
                    _mvcStartUpContent.UseStatements.Add(@"    routes.MapRoute(");
                    _mvcStartUpContent.UseStatements.Add(@"        name: ""default"",");
                    _mvcStartUpContent.UseStatements.Add(@"        template: ""{controller}/{action}/{id?}"",");
                    _mvcStartUpContent.UseStatements.Add(@"        defaults: new { controller = ""Home"", action = ""Index"" });");
                    _mvcStartUpContent.UseStatements.Add("");
                    _mvcStartUpContent.UseStatements.Add(@"    routes.MapRoute(");
                    _mvcStartUpContent.UseStatements.Add(@"        name: ""api"",");
                    _mvcStartUpContent.UseStatements.Add(@"        template: ""{controller}/{id?}"");");
                    _mvcStartUpContent.UseStatements.Add(@"});");
                }
                return _mvcStartUpContent;
            }
        }
    }
}