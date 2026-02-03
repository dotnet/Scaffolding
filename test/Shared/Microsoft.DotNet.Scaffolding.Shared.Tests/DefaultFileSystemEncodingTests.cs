// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.DotNet.Scaffolding.Shared;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class DefaultFileSystemEncodingTests
    {
        [Fact]
        public void WriteAllText_UsesUtf8EncodingWithoutBom()
        {
            // Arrange
            var fileSystem = new DefaultFileSystem();
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            var contentWithNonAscii = "Hello мир 世界 العالم"; // Russian, Chinese, Arabic
            
            try
            {
                // Act
                fileSystem.WriteAllText(tempFile, contentWithNonAscii);
                
                // Assert
                var bytes = File.ReadAllBytes(tempFile);
                
                // Check that file does NOT start with UTF-8 BOM (EF BB BF)
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    "File should not contain UTF-8 BOM");
                
                // Check that content can be read correctly as UTF-8
                var readContent = File.ReadAllText(tempFile, Encoding.UTF8);
                Assert.Equal(contentWithNonAscii, readContent);
                
                // Verify encoding by reading with UTF8 encoding explicitly
                using (var reader = new StreamReader(tempFile, new UTF8Encoding(false)))
                {
                    var content = reader.ReadToEnd();
                    Assert.Equal(contentWithNonAscii, content);
                }
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void WriteAllText_PreservesNonAsciiCharacters()
        {
            // Arrange
            var fileSystem = new DefaultFileSystem();
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            var testCases = new[]
            {
                "Русский текст", // Russian
                "中文文本", // Chinese
                "النص العربي", // Arabic
                "Ελληνικό κείμενο", // Greek
                "日本語のテキスト", // Japanese
                "한국어 텍스트", // Korean
                "Émojis: 😀🎉🌟" // Emojis
            };
            
            try
            {
                foreach (var testContent in testCases)
                {
                    // Act
                    fileSystem.WriteAllText(tempFile, testContent);
                    
                    // Assert
                    var readContent = File.ReadAllText(tempFile, Encoding.UTF8);
                    Assert.Equal(testContent, readContent);
                }
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void WriteAllText_DoesNotUseSystemDefaultEncoding()
        {
            // Arrange
            var fileSystem = new DefaultFileSystem();
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            var russianText = "Привет мир";
            
            try
            {
                // Act
                fileSystem.WriteAllText(tempFile, russianText);
                
                // Assert - Try reading with default encoding (which might be different on different systems)
                // If the file was written with UTF-8, it should read correctly
                var bytes = File.ReadAllBytes(tempFile);
                var utf8Content = Encoding.UTF8.GetString(bytes);
                Assert.Equal(russianText, utf8Content);
                
                // Verify it's not using some other encoding like Windows-1251
                // If it were Windows-1251, these bytes would be different
                var utf8Bytes = new UTF8Encoding(false).GetBytes(russianText);
                Assert.Equal(utf8Bytes, bytes);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
