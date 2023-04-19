// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency
{
    public class StartupContent
    {
        private SortedSet<string> _requiredNamespaces = new SortedSet<string>(StringComparer.Ordinal);
        private List<string> _serviceStatements = new List<string>();
        private List<string> _useStatements = new List<string>();

        public SortedSet<string> RequiredNamespaces
        {
            get
            {
                return _requiredNamespaces;
            }
        }

        public List<string> ServiceStatements
        {
            get
            {
                return _serviceStatements;
            }
        }

        public List<string> UseStatements
        {
            get
            {
                return _useStatements;
            }
        }
    }
}
