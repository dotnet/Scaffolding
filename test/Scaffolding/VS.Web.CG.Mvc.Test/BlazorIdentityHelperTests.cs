// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class BlazorIdentityHelperTests
    {
        [SkippableTheory]
        [MemberData(nameof(BlazorIdentityFiles))]
        public void GetFormattedRelativeIdentityFileTest(string fullFileName, string expected)
        {
            Skip.If(!OperatingSystem.IsWindows());
            // Arrange & Act
            string result = BlazorIdentityHelper.GetFormattedRelativeIdentityFile(fullFileName);
            // Assert
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> BlazorIdentityFiles
        {
            get
            {
                return new[]
                {
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\file.tt", "file" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\file.tt", "Pages.file" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\Manage\\file.tt", "Pages.Manage.file" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\Manage\\Passkeys.tt", "Pages.Manage.Passkeys" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Pages\\Manage\\RenamePasskey.tt", "Pages.Manage.RenamePasskey" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Shared\\PasskeySubmit.tt", "Shared.PasskeySubmit" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\PasskeyInputModel.tt", "PasskeyInputModel" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\PasskeyOperation.tt", "PasskeyOperation" },
                    new object[] { "C:\\Some\\Path\\Templates\\BlazorIdentity\\Thing\\file.tt", "Thing.file" },
                    new object[] { "C:\\Some\\Path\\Templates\\Thing\\file.tt", "" },
                };
            }
        }
    }
}
