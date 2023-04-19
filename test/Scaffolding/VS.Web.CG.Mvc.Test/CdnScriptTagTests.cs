﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class CdnScriptTagTests
    {
        private readonly ITestOutputHelper _output;

        public CdnScriptTagTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ScriptTags_SubresourceIntegrityCheck()
        {
            var slnDir = GetSolutionDir();
            var sourceDir = Path.Combine(slnDir, "src", "Scaffolding", "VS.Web.CG.Mvc");
            var cshtmlFiles = Directory.GetFiles(sourceDir, "*.cshtml", SearchOption.AllDirectories);

            var scriptTags = new List<ScriptTag>();
            foreach (var cshtmlFile in cshtmlFiles)
            {
                scriptTags.AddRange(GetScriptTags(cshtmlFile));
            }

            Assert.NotEmpty(scriptTags);

            var shasum = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var client = new HttpClient())
            {
                foreach (var script in scriptTags)
                {
                    if (shasum.ContainsKey(script.Src))
                    {
                        continue;
                    }

                    using (var resp = await client.GetStreamAsync(script.Src))
                    using (var alg = SHA384.Create())
                    {
                        var hash = alg.ComputeHash(resp);
                        shasum.Add(script.Src, "sha384-" + Convert.ToBase64String(hash));
                    }
                }
            }

            Assert.All(scriptTags, t =>
            {
                Assert.True(shasum[t.Src] == t.Integrity, userMessage: $"Expected integrity on script tag to be {shasum[t.Src]} but it was {t.Integrity}: {t.Path}");
            });
        }

        private struct ScriptTag
        {
            public string Src;
            public string Integrity;
            public string FileName;
            internal string Path;
        }

        private static readonly Regex _scriptRegex = new Regex(@"<script[^>]*src=""(?'src'http[^""]+)""[^>]*integrity=""(?'integrity'[^""]+)""([^>]*)>", RegexOptions.Multiline);

        private IEnumerable<ScriptTag> GetScriptTags(string cshtmlFile)
        {
            string contents;
            using (var reader = new StreamReader(File.OpenRead(cshtmlFile)))
            {
                contents = reader.ReadToEnd();
            }

            var match = _scriptRegex.Match(contents);
            while (match != null && match != Match.Empty)
            {
                var tag = new ScriptTag
                {
                    Src = match.Groups["src"].Value,
                    Integrity = match.Groups["integrity"].Value,
                    FileName = Path.GetFileName(cshtmlFile),
                    Path = cshtmlFile,
                };
                yield return tag;
                _output.WriteLine($"Found script tag in '{tag.FileName}', src='{tag.Src}' integrity='{tag.Integrity}'");
                match = match.NextMatch();
            }
        }

        private static string GetSolutionDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "All.sln")))
                {
                    break;
                }
                dir = dir.Parent;
            }
            return dir.FullName;
        }
    }
}
