// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class RoslynUtilitiesTests : DocumentBuilderTestBase
    {
        [Theory, MemberData(nameof(TestData))]
        public void TestCreateEscapedIdentifier(string identifier, string expectedValue)
        {
            Assert.Equal(expectedValue, RoslynUtilities.CreateEscapedIdentifier(identifier));
        }

        [Theory, MemberData(nameof(NameSpaceTestData))]
        public void TestCreateValidNameSpace(string identifier, bool expectedValue)
        {
            Assert.Equal(expectedValue, RoslynUtilities.IsValidNamespace(identifier));
        }

        [Fact]
        public async Task CheckDocumentForMethodInvocationAsyncTest()
        {
            //Arrange
            var programCsDoc = CreateDocument(ConsoleAppProgramCsFile);
            //Act, only check1 should be true
            var check1 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, "ToUpper", "string");
            var check2 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, string.Empty, "Microsoft.AspNetCore.Builder.IApplicationBuilder");
            var check3 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, null, "Microsoft.AspNetCore.Builder.IApplicationBuilder");
            var check4 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, string.Empty, string.Empty);
            var check5 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, null, null);
            var check6 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, "UseAuthorization", string.Empty);
            var check7 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, "UseAuthorization", null);
            var check8 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, "UseAuthorization", "String");
            var check9 = await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programCsDoc, "sampleString.ToUpper()", "int");
            //Assert
            Assert.True(check1);
            Assert.False(check2);
            Assert.False(check3);
            Assert.False(check4);
            Assert.False(check5);
            Assert.False(check6);
            Assert.False(check7);
            Assert.False(check8);
            Assert.False(check9);
        }

        [Fact]
        public async Task CheckDocumentForTextAsyncTest()
        {
            //Arrange
            var programCsDoc = CreateDocument(MinimalProgramCsFile);
            var net8CsprojDoc = CreateDocument(Net8Csproj);
            //Act
            var check1 = await RoslynUtilities.CheckDocumentForTextAsync(programCsDoc, "app.UseHsts()");
            var check2 = await RoslynUtilities.CheckDocumentForTextAsync(programCsDoc, "app.UseEndpoints()");
            var check3 = await RoslynUtilities.CheckDocumentForTextAsync(net8CsprojDoc, "net8.0");
            var check4 = await RoslynUtilities.CheckDocumentForTextAsync(net8CsprojDoc, "net7.0");
            //Assert
            Assert.True(check1);
            Assert.True(check3);
            Assert.False(check2);
            Assert.False(check4);

        }

        [Theory, MemberData(nameof(KeyWordTestData))]
        public void IsKeyWordTests(string keyword, bool isKeyword)
        {
            Assert.Equal(isKeyword, RoslynUtilities.IsKeyWord(keyword));
        }

        public static IEnumerable<object[]> NameSpaceTestData
        {
            get
            {
                return new[]
                {
                    new object[] {"dotnet-aspnet-codegenerator", false},
                    new object[] {"abc def", false },
                    new object[] {"abc.def ghi.xyz", false},
                    new object[] {"..abc",false},
                    new object[] {"abc.@xyz", false},
                    new object[] {"$abd", false},
                    new object[] {"namespace", false},
                    new object[] {"class", false},
                    new object[] {"9abc", false},
                    new object[] {"9.abc", false},
                    new object[] {"ab.c9.de", true},
                    new object[] {"abc.def", true},
                    new object[] {"validnamespace", true}
                };
            }
        }

        public static IEnumerable<object[]> KeyWordTestData
        {
            get
            {
                return new[]
                {
                    //Keywords
                    new object[] {"using", true},
                    new object[] {"virtual", true},
                    new object[] {"void", true},
                    new object[] {"volatile", true},
                    new object[] {"while", true},
                    // Non Keywords
                    new object[] {"i", false},
                    new object[] {"Class", false},
                    new object[] {"Model", false},
                    new object[] {"FALSE", false},
                    new object[] {"TRUE", false},
                    new object[] {"blah", false},
                };
            }
        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                return new[]
                {
                    new object[] {"abstract", "@abstract"},
                    new object[] {"as", "@as"},
                    new object[] {"base", "@base"},
                    new object[] {"bool", "@bool"},
                    new object[] {"break", "@break"},
                    new object[] {"byte", "@byte"},
                    new object[] {"case", "@case"},
                    new object[] {"catch", "@catch"},
                    new object[] {"char", "@char"},
                    new object[] {"checked", "@checked"},
                    new object[] {"class", "@class"},
                    new object[] {"const", "@const"},
                    new object[] {"continue", "@continue"},
                    new object[] {"decimal", "@decimal"},
                    new object[] {"default", "@default"},
                    new object[] {"delegate", "@delegate"},
                    new object[] {"do", "@do"},
                    new object[] {"double", "@double"},
                    new object[] {"else", "@else"},
                    new object[] {"enum", "@enum"},
                    new object[] {"event", "@event"},
                    new object[] {"explicit", "@explicit"},
                    new object[] {"extern", "@extern"},
                    new object[] {"finally", "@finally"},
                    new object[] {"fixed", "@fixed"},
                    new object[] {"float", "@float"},
                    new object[] {"for", "@for"},
                    new object[] {"foreach", "@foreach"},
                    new object[] {"goto", "@goto"},
                    new object[] {"if", "@if"},
                    new object[] {"implicit", "@implicit"},
                    new object[] {"in", "@in"},
                    new object[] {"int", "@int"},
                    new object[] {"interface", "@interface"},
                    new object[] {"internal", "@internal"},
                    new object[] {"is", "@is"},
                    new object[] {"lock", "@lock"},
                    new object[] {"long", "@long"},
                    new object[] {"namespace", "@namespace"},
                    new object[] {"new", "@new"},
                    new object[] {"null", "@null"},
                    new object[] {"object", "@object"},
                    new object[] {"operator", "@operator"},
                    new object[] {"out", "@out"},
                    new object[] {"override", "@override"},
                    new object[] {"params", "@params"},
                    new object[] {"private", "@private"},
                    new object[] {"protected", "@protected"},
                    new object[] {"public", "@public"},
                    new object[] {"readonly", "@readonly"},
                    new object[] {"ref", "@ref"},
                    new object[] {"return", "@return"},
                    new object[] {"sbyte", "@sbyte"},
                    new object[] {"sealed", "@sealed"},
                    new object[] {"short", "@short"},
                    new object[] {"sizeof", "@sizeof"},
                    new object[] {"stackalloc", "@stackalloc"},
                    new object[] {"static", "@static"},
                    new object[] {"string", "@string"},
                    new object[] {"struct", "@struct"},
                    new object[] {"switch", "@switch"},
                    new object[] {"this", "@this"},
                    new object[] {"throw", "@throw"},
                    new object[] {"try", "@try"},
                    new object[] {"typeof", "@typeof"},
                    new object[] {"uint", "@uint"},
                    new object[] {"ulong", "@ulong"},
                    new object[] {"unchecked", "@unchecked"},
                    new object[] {"unsafe", "@unsafe"},
                    new object[] {"ushort", "@ushort"},
                    new object[] {"using", "@using"},
                    new object[] {"virtual", "@virtual"},
                    new object[] {"void", "@void"},
                    new object[] {"volatile", "@volatile"},
                    new object[] {"while", "@while"},
                    // Non Keywords
                    new object[] {"i", "i"},
                    new object[] {"Class", "Class"},
                    new object[] {"Model", "Model"},
                    new object[] {"FALSE", "FALSE"},
                    new object[] {"TRUE", "TRUE"}
                };
            }
        }
    }
}
