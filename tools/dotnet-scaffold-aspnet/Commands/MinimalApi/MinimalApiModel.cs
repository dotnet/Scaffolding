// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi
{
    public class MinimalApiModel
    {
        //Endpoints class name
        public string? EndpointsName { get; set; }
        public string EndpointsFileName { get; set; } = default!;
        public string? EndpointsPath { get; set; }
        public string? DbContextClassName { get; set; }
        public string? DbContextClassPath { get; set; }
        public string? DatabaseProvider  { get; set; }
        public string? EntitySetName { get; set; }
        public string? PrimaryKeyName { get; set; }
        public string? PrimaryKeyShortTypeName { get; set; }
        public string? PrimaryKeyTypeName { get; set; }
        public bool EfScenario { get; set; }
        public bool NullableEnabled { get; set; }
        public List<string>? ModelProperties { get; set; }
        public string? ModelNamespace { get; set; }

        //If CRUD endpoints support Open API
        public bool OpenAPI { get; set; }

        //Use TypedResults for minimal apis.
        public bool UseTypedResults { get; set; }

        //Generated namespace for a Endpoints class/file. If using an existing file, does not apply.
        public string? EndpointsNamespace { get; set; }

        //Method name for the new static method with the CRUD endpoints.
        public string? MethodName { get; set; }

        //Model class' name
        public string ModelTypeName { get; set; } = default!;

        private string? _modelTypePluralName;
        public string ModelTypePluralName
        {
            get
            {
                if (_modelTypePluralName == null)
                {
                    _modelTypePluralName = "plurals";
                }

                return _modelTypePluralName;
            }
        }

        //Model class' name but lower case
        public string ModelVariable
        {
            get
            {
                return "";
            }
        }

        //variable name holding the models in the DbContext class
        public string EntitySetVariable
        {
            get
            {
                return "";
            }
        }

        public string? DbContextNamespace { get; private set; }
        public string? RelativeFolderPath { get; internal set; }
    }
}
