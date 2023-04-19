// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    public interface ITemplating
    {
        Task<TemplateResult> RunTemplateAsync(string content, dynamic templateModel);
    }
}
