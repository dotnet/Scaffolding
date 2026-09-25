// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Tests
{
    public class FileSystemEncodingTests
    {
        [Fact]
        public void WriteAllText_UsesUtf8EncodingWithoutBom()
        {
            // Arrange
            var fileSystem = new FileSystem();
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
        public void WriteAllLines_UsesUtf8EncodingWithoutBom()
        {
            // Arrange
            var fileSystem = new FileSystem();
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            var linesWithNonAscii = new[]
            {
                "Русский текст", // Russian
                "中文文本", // Chinese
                "النص العربي" // Arabic
            };
            
            try
            {
                // Act
                fileSystem.WriteAllLines(tempFile, linesWithNonAscii);
                
                // Assert
                var bytes = File.ReadAllBytes(tempFile);
                
                // Check that file does NOT start with UTF-8 BOM (EF BB BF)
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    "File should not contain UTF-8 BOM");
                
                // Check that content can be read correctly as UTF-8
                var readLines = File.ReadAllLines(tempFile, Encoding.UTF8);
                Assert.Equal(linesWithNonAscii, readLines);
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
            var fileSystem = new FileSystem();
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            var testCases = new[]
            {
                "Привет мир", // Russian (the original issue scenario)
                "日本語", // Japanese
                "한국어", // Korean
                "Émojis: 🚀💻🎉" // Emojis
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
                    
                    // Verify the bytes are correct UTF-8
                    var expectedBytes = new UTF8Encoding(false).GetBytes(testContent);
                    var actualBytes = File.ReadAllBytes(tempFile);
                    Assert.Equal(expectedBytes, actualBytes);
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
        public void WriteAllLines_PreservesNonAsciiCharacters()
        {
            // Arrange
            var fileSystem = new FileSystem();
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            var linesWithSpecialChars = new[]
            {
                "Line 1: Привет",
                "Line 2: 你好",
                "Line 3: مرحبا",
                "Line 4: Hello 🌍"
            };
            
            try
            {
                // Act
                fileSystem.WriteAllLines(tempFile, linesWithSpecialChars);
                
                // Assert
                var readLines = File.ReadAllLines(tempFile, Encoding.UTF8);
                Assert.Equal(linesWithSpecialChars, readLines);
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
        public void WriteAllText_DoesNotUseWindowsDefaultEncoding()
        {
            // Arrange
            var fileSystem = new FileSystem();
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.txt");
            // This text would be corrupted if written with Windows-1251 encoding
            var russianText = "Этот текст должен быть читаемым";
            
            try
            {
                // Act
                fileSystem.WriteAllText(tempFile, russianText);
                
                // Assert
                var bytes = File.ReadAllBytes(tempFile);
                var utf8Bytes = new UTF8Encoding(false).GetBytes(russianText);
                
                // The bytes should match UTF-8 encoding, not Windows-1251 or any other encoding
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
