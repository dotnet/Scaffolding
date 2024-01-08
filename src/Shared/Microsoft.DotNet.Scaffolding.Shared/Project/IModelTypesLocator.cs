// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
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
        Task<IEnumerable<ITypeSymbol>> GetAllTypesAsync();

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
