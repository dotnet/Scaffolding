// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    public class StartupContent
    {
        private SortedSet<string> _requiredNamespaces = new SortedSet<string>(StringComparer.Ordinal);
        private List<string> _serviceStatements = new List<string>();
        private List<string> _useStatements = new List<string>();

        public IEnumerable<string> RequiredNamespaces
        {
            get
            {
                return _requiredNamespaces;
            }
        }

        public IEnumerable<string> ServiceStatements
        {
            get
            {
                return _serviceStatements;
            }
        }

        public IEnumerable<string> UseStatements
        {
            get
            {
                return _useStatements;
            }
        }

        public void AddRequiredNamespace(string @namespace)
        {
            _requiredNamespaces.Add(@namespace);
        }

        public void AddServiceStatement(string statement)
        {
            _serviceStatements.Add(statement);
        }

        public void AddUseStatement(string statement)
        {
            _useStatements.Add(statement);
        }
    }
}