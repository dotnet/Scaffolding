// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface IDbContextEditorServices
    {
        Task<SyntaxTree> AddNewContext(NewDbContextTemplateModel dbContextTemplateModel);

        [Obsolete]
        EditSyntaxTreeResult AddModelToContext(ModelType dbContext, ModelType modelType, bool nullableEnabled);

        [Obsolete]
        EditSyntaxTreeResult EditStartupForNewContext(ModelType startup, string dbContextTypeName, string dbContextNamespace, string dataBaseName, bool useSqlite, bool useTopLevelStatements);

        EditSyntaxTreeResult AddModelToContext(ModelType dbContext, ModelType modelType, IDictionary<string, string> parameters);
        EditSyntaxTreeResult EditStartupForNewContext(ModelType startup, IDictionary<string, string> parameters);
    }
}
