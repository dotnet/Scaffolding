using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Shared.T4Templating;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests.T4TemplatingTests
{
    public class TemplateInvokerTests
    {

        [Fact]
        public void TestInvokeTemplate()
        {
            TemplateInvoker templateInvoker = new TemplateInvoker();
            var movieModel = new TestMovieModel()
            {
                ID = 1,
                Genre = "Boring",
                Price = 10.00M,
                Title = "A Very Boring Love Story"
            };

            var dictParams = new Dictionary<string, object>()
            {
                { "Model" , movieModel }
            };

            ITextTransformation contextTemplate = GetTestTransformation();
            var templatedString = templateInvoker.InvokeTemplate(contextTemplate, dictParams);
            Assert.False(string.IsNullOrEmpty(templatedString));
            Assert.Equal(ExpectedTemplatedString.Replace("\r\n", "\n"), templatedString.Replace("\r\n", "\n"));
        }

        private ITextTransformation GetTestTransformation()
        {
            var testOutputDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var templatePath = Path.Combine(testOutputDir, "T4TemplatingTests", "MoviePage.tt");
            Assert.True(Path.Exists(templatePath), "Did not find T4 template to run the test");
            var host = new TextTemplatingEngineHost { TemplateFile = templatePath };
            var contextTemplate = new MoviePage
            {
                Host = host,
                Session = host.CreateSession()
            };

            return contextTemplate;
        }

        private string ExpectedTemplatedString =
@"ID : 1
Title : A Very Boring Love Story
Genre : Boring
Price : 10.00
";
    }

    internal class TestMovieModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public decimal Price { get; set; }
    }
}
