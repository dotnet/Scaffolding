using Microsoft.DotNet.MSIdentity.MicrosoftIdentityPlatformApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.MSIdentity.UnitTests.Tests
{
    public class MicrosoftIdentityPlatformApplicationManagerTests
    {
        [Theory]
        [MemberData(nameof(UriList))]
        public void ValidateUrisTests(List<string> urisToValidate, List<string> validUris)
        {
            var validatedUris = MicrosoftIdentityPlatformApplicationManager.ValidateUris(urisToValidate);
            var uriStrings = validatedUris.Select(uri => uri.ToString()).ToList();
            var areEquivalent = (validUris.Count == validatedUris.Count) && !uriStrings.Except(validUris).Any();
            Assert.True(areEquivalent);
        }

        public static IEnumerable<object[]> UriList =>
            new List<object[]>
            {
                new object[] {
                    new List<string> {
                                        "https://localhost:5001/",
                                        "https://localhost:5002/get",
                                        "http://localhost:5001/",
                                        "https://www.microsoft.com/",
                                        "http://www.azure.com",
                                        "https://www.testapi.com/get/{id}",
                                        "http://www.skype.com",
                                        "http://127.0.0.1/get",
                                        "http://loopback/post",
                                        "badstring",
                                        null,
                                        string.Empty,
                                        ""
                                     },

                    new List<string> {
                                        "https://localhost:5001/",
                                        "https://localhost:5002/get",
                                        "http://localhost:5001/",
                                        "https://www.microsoft.com/",
                                        "https://www.testapi.com/get/{id}",
                                        "http://127.0.0.1/get",
                                        "http://localhost/post",
                                     }
                }
            };
    }
}
