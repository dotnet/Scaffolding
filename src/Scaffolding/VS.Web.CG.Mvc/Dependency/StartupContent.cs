// Copyright (c) .NET Foundation. All rights reserved.

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
