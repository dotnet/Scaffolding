// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.MinimalApi;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class BlazorWebCRUDGeneratorCommandLineModelTests
    {
        [Fact]
        public void ValidateModelTests()
        {
            BlazorWebCRUDGeneratorCommandLineModel model = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
                TemplateName = "crud"
            };

            BlazorWebCRUDGeneratorCommandLineModel emptyModelName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                TemplateName = "create"
            };

            BlazorWebCRUDGeneratorCommandLineModel emptyTemplateName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
            };

            BlazorWebCRUDGeneratorCommandLineModel invalidTemplateName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
                TemplateName = "templateName"
            };

            BlazorWebCRUDGeneratorCommandLineModel invalidNamespaceName = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "className",
                TemplateName = "crud",
                Namespace = "using"
            };

            Action modelAction = () => model.ValidateCommandline();
            Action emptyModelNameAction = () => emptyModelName.ValidateCommandline();
            Action emptyTemplateNameAction = () => emptyTemplateName.ValidateCommandline();
            Action invalidTemplateNameAction = () => invalidTemplateName.ValidateCommandline();
            Action invalidNamespaceNameAction = () => invalidNamespaceName.ValidateCommandline();
            //assert
            var noException = Record.Exception(modelAction);
            var exception = Assert.Throws<ArgumentNullException>(emptyModelNameAction);
            var exception2 = Record.Exception(emptyTemplateNameAction);
            var exception3 = Assert.Throws<InvalidOperationException>(invalidTemplateNameAction);
            var exception4 = Assert.Throws<InvalidOperationException>(invalidNamespaceNameAction);
        }
    }
}
