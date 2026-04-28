// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.BlazorIdentity;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class BlazorIdentityHelperTests
    {
        [Theory]
        [MemberData(nameof(BlazorIdentityFiles))]
        public void GetFormattedRelativeIdentityFileTest(string fullFileName, string expected)
        {
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
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "file.tt"), "file" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "Pages", "file.tt"), "Pages.file" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "Pages", "Manage", "file.tt"), "Pages.Manage.file" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "Pages", "Manage", "Passkeys.tt"), "Pages.Manage.Passkeys" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "Pages", "Manage", "RenamePasskey.tt"), "Pages.Manage.RenamePasskey" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "Shared", "PasskeySubmit.tt"), "Shared.PasskeySubmit" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "PasskeyInputModel.tt"), "PasskeyInputModel" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "PasskeyOperation.tt"), "PasskeyOperation" },
                    new object[] { Path.Combine("some", "path", "Templates", "BlazorIdentity", "Thing", "file.tt"), "Thing.file" },
                    new object[] { Path.Combine("some", "path", "Templates", "Thing", "file.tt"), "" },
                };
            }
        }
    }
}
