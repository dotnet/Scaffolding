// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity
{
    /// <summary>
    /// Model for Razor templates to create or add to existing Endpoints file for the 'dotnet-aspnet-codegenerator minimalapi' scenario 
    /// </summary>
    public class BlazorIdentityModel
    {
        public BlazorIdentityModel()
        {}

        public string BlazorIdentityNamespace { get; set; }
        public string BlazorLayoutNamespace { get; set; }
        public string UserClassName { get; internal set; }
        public string UserClassNamespace { get; internal set; }
        public string DbContextNamespace { get; set; }
        public string DbContextName { get; set; }
        //Database type eg. SQL Server or SQLite
        public DbProvider DatabaseProvider { get; set; }
        public List<string> FilesToGenerate { get; set; }
        public string BaseOutputPath { get; set; }
        // The root namespace used for computing output file paths. Defaults to the server project's
        // root namespace, but overridden to the client project's namespace for WASM/Auto Global projects.
        public string RootNamespace { get; set; }
    }
}
