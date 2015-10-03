// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Extensions.CodeGeneration.Templating
{
    public interface ITemplating
    {
        Task<TemplateResult> RunTemplateAsync(string content, dynamic templateModel);
    }
}