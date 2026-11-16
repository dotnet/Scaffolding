// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Text helpers for the markup files that scaffolders edit as plain text (host pages, _Imports.razor).
/// </summary>
internal static class MarkupHelper
{
    private const string HtmlCommentStart = "<!--";
    private const string HtmlCommentEnd = "-->";
    private const string RazorCommentStart = "@*";
    private const string RazorCommentEnd = "*@";

    /// <summary>
    /// Returns true when <paramref name="path"/> is a Razor file (.razor or .cshtml), in which <c>@* ... *@</c> is a comment.
    /// </summary>
    internal static bool IsRazorFile(string? path)
        => !string.IsNullOrEmpty(path) &&
           (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns <paramref name="content"/> with the text of every comment replaced by spaces, so that code inside comments is
    /// not mistaken for active code. Line breaks are kept and the length is unchanged, so an index into the result is the
    /// same index into <paramref name="content"/>. A comment that is never closed is left as is.
    /// </summary>
    /// <param name="content">The markup to mask.</param>
    /// <param name="htmlComments">Mask HTML comments (<c>&lt;!-- ... --&gt;</c>).</param>
    /// <param name="razorComments">Mask Razor comments (<c>@* ... *@</c>).</param>
    internal static string MaskComments(string? content, bool htmlComments, bool razorComments)
    {
        if (string.IsNullOrEmpty(content) || (!htmlComments && !razorComments))
        {
            return content ?? string.Empty;
        }

        var masked = new StringBuilder(content);
        var index = 0;
        while (index < content.Length)
        {
            var htmlStart = htmlComments ? content.IndexOf(HtmlCommentStart, index, StringComparison.Ordinal) : -1;
            var razorStart = razorComments ? content.IndexOf(RazorCommentStart, index, StringComparison.Ordinal) : -1;
            var isRazor = razorStart >= 0 && (htmlStart < 0 || razorStart < htmlStart);
            var start = isRazor ? razorStart : htmlStart;
            if (start < 0)
            {
                break;
            }

            var (opening, closing) = isRazor ? (RazorCommentStart, RazorCommentEnd) : (HtmlCommentStart, HtmlCommentEnd);
            var closingIndex = content.IndexOf(closing, start + opening.Length, StringComparison.Ordinal);
            if (closingIndex < 0)
            {
                break;
            }

            var end = closingIndex + closing.Length;
            for (var i = start; i < end; i++)
            {
                if (content[i] is not ('\r' or '\n'))
                {
                    masked[i] = ' ';
                }
            }

            index = end;
        }

        return masked.ToString();
    }

    /// <summary>
    /// Masks the comments that apply to the file at <paramref name="path"/>: HTML comments, and Razor comments in a
    /// .razor or .cshtml file. See <see cref="MaskComments(string?, bool, bool)"/>.
    /// </summary>
    internal static string MaskComments(string? path, string? content)
        => MaskComments(content, htmlComments: true, razorComments: IsRazorFile(path));

    /// <summary>
    /// Detects the dominant line ending of the given text ("\r\n" or "\n").
    /// </summary>
    internal static string DetectLineEnding(string? content)
        => !string.IsNullOrEmpty(content) && content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
}
