// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Shared.Project
{
    public interface IModelTypesLocator
    {
        /// <summary>
        /// returns all Documents in all the projects in the solution.
        /// </summary>
        IEnumerable<Document> GetAllDocuments();
        IEnumerable<ModelType> GetAllTypes();

        /// <summary>
        /// Returns the types matching a type name.
        /// The method returns types for which the
        /// full name matches the given typeName exactly.
        /// However, if there are none exactly matching, then
        /// it returns all the types whose type name (without the namespace name)
        /// matches the given type name. This allows the callers
        /// to pass in a short type name and get the results.
        /// Caller has to decide what to do when there are multiple matches.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        IEnumerable<ModelType> GetType(string typeName);
    }
}
