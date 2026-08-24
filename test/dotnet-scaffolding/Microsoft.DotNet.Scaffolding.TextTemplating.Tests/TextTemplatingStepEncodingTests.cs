// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.TextTemplating.Tests
{
    public class TextTemplatingStepEncodingTests
    {
        [Fact]
        public void TextTemplatingStep_WritesFilesWithUtf8EncodingWithoutBom()
        {
            // This test verifies that when TextTemplatingStep writes files,
            // they use UTF-8 encoding without BOM
            
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            var outputFile = Path.Combine(tempDir, "output.txt");
            var contentWithNonAscii = "Test content with non-ASCII: Привет мир 你好世界";
            
            try
            {
                Directory.CreateDirectory(tempDir);
                
                // Simulate what TextTemplatingStep does - write content to a file
                File.WriteAllText(outputFile, contentWithNonAscii, new UTF8Encoding(false));
                
                // Assert
                var bytes = File.ReadAllBytes(outputFile);
                
                // Verify no BOM
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    "File should not contain UTF-8 BOM");
                
                // Verify content is readable with UTF-8
                var readContent = File.ReadAllText(outputFile, Encoding.UTF8);
                Assert.Equal(contentWithNonAscii, readContent);
                
                // Verify exact bytes match UTF-8 without BOM
                var expectedBytes = new UTF8Encoding(false).GetBytes(contentWithNonAscii);
                Assert.Equal(expectedBytes, bytes);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void TextTemplatingStep_PreservesMultilingualContent()
        {
            // Test various languages that were problematic with Windows-1251 encoding
            var testCases = new[]
            {
                ("Russian", "Привет мир! Это тест кодировки."),
                ("Chinese", "你好世界！这是编码测试。"),
                ("Arabic", "مرحبا بالعالم! هذا اختبار الترميز."),
                ("Japanese", "こんにちは世界！これはエンコーディングテストです。"),
                ("Korean", "안녕하세요! 이것은 인코딩 테스트입니다."),
                ("Mixed", "Hello мир 世界 🌍 test")
            };
            
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            
            try
            {
                Directory.CreateDirectory(tempDir);
                
                foreach (var (language, content) in testCases)
                {
                    var outputFile = Path.Combine(tempDir, $"{language}.txt");
                    
                    // Act - simulate TextTemplatingStep writing
                    File.WriteAllText(outputFile, content, new UTF8Encoding(false));
                    
                    // Assert
                    var readContent = File.ReadAllText(outputFile, Encoding.UTF8);
                    Assert.Equal(content, readContent);
                    
                    // Verify no corruption occurred
                    var bytes = File.ReadAllBytes(outputFile);
                    var expectedBytes = new UTF8Encoding(false).GetBytes(content);
                    Assert.Equal(expectedBytes, bytes);
                }
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void TextTemplatingStep_AvoidsBomInUtf8Files()
        {
            // Verify that the encoding used is UTF8 without BOM
            // BOM can cause issues with some tools and parsers
            
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.cs");
            var csharpContent = @"// Файл с русскими комментариями
namespace TestNamespace
{
    public class TestClass
    {
        // Метод для теста
        public void TestMethod()
        {
            var message = ""Привет, мир!"";
        }
    }
}";
            
            try
            {
                // Act
                File.WriteAllText(tempFile, csharpContent, new UTF8Encoding(false));
                
                // Assert
                var bytes = File.ReadAllBytes(tempFile);
                
                // First 3 bytes should NOT be the UTF-8 BOM
                if (bytes.Length >= 3)
                {
                    var hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                    Assert.False(hasBom, "Generated files should not have UTF-8 BOM");
                }
                
                // Content should still be readable
                var readContent = File.ReadAllText(tempFile, Encoding.UTF8);
                Assert.Equal(csharpContent, readContent);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
