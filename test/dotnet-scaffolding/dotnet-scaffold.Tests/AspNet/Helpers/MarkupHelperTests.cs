// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class MarkupHelperTests
{
    [Theory]
    [InlineData("a <!-- b --> c", "a            c")]
    [InlineData("a @* b *@ c", "a         c")]
    [InlineData("@*\r\n@using X\r\n*@\r\n@using Y", "  \r\n        \r\n  \r\n@using Y")]
    [InlineData("<!-- @* -->x*@", "           x*@")]
    [InlineData("@* <!-- *@x-->", "          x-->")]
    public void MaskComments_BlanksCommentsAndKeepsLineBreaksAndLength(string content, string expected)
    {
        var masked = MarkupHelper.MaskComments(content, htmlComments: true, razorComments: true);

        Assert.Equal(expected, masked);
        Assert.Equal(content.Length, masked.Length);
    }

    [Theory]
    [InlineData("a <!-- b")]
    [InlineData("a @* b")]
    public void MaskComments_LeavesUnclosedCommentsAsIs(string content)
        => Assert.Equal(content, MarkupHelper.MaskComments(content, htmlComments: true, razorComments: true));

    [Fact]
    public void MaskComments_MasksOnlyTheRequestedCommentKinds()
    {
        const string content = "<!-- a --> @* b *@";

        Assert.Equal("           @* b *@", MarkupHelper.MaskComments(content, htmlComments: true, razorComments: false));
        Assert.Equal("<!-- a -->        ", MarkupHelper.MaskComments(content, htmlComments: false, razorComments: true));
    }

    [Theory]
    [InlineData("App.razor", "<!-- a --> @* b *@", "                  ")]
    [InlineData("_Host.cshtml", "<!-- a --> @* b *@", "                  ")]
    [InlineData("index.html", "<!-- a --> @* b *@", "           @* b *@")]
    [InlineData(null, "<!-- a --> @* b *@", "           @* b *@")]
    public void MaskComments_ForAFile_MasksRazorCommentsOnlyInRazorFiles(string? path, string content, string expected)
        => Assert.Equal(expected, MarkupHelper.MaskComments(path, content));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskComments_ReturnsEmptyForNoContent(string? content)
        => Assert.Equal(string.Empty, MarkupHelper.MaskComments(content, htmlComments: true, razorComments: true));

    [Theory]
    [InlineData("a\r\nb\r\n", "\r\n")]
    [InlineData("a\nb\n", "\n")]
    [InlineData("", "\n")]
    [InlineData(null, "\n")]
    public void DetectLineEnding_ReturnsDominantLineEnding(string? content, string expected)
        => Assert.Equal(expected, MarkupHelper.DetectLineEnding(content));
}
