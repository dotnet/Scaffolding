// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface IDbContextEditorServices
    {
        Task<SyntaxTree> AddNewContext(NewDbContextTemplateModel dbContextTemplateModel);

        EditSyntaxTreeResult AddModelToContext(ModelType dbContext, ModelType modelType);

        EditSyntaxTreeResult EditStartupForNewContext(ModelType startup, string dbContextTypeName, string dbContextNamespace, string dataBaseName, bool useSqlite);
    }
}