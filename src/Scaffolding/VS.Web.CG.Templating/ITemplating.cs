// Copyright (c) .NET Foundation. All rights reserved.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    public interface ITemplating
    {
        Task<TemplateResult> RunTemplateAsync(string content, dynamic templateModel);
    }
}
