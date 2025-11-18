// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Represents the model for Razor Page scaffolding, providing helpers for input types and classes in templates.
/// </summary>
internal class RazorPageModel : CrudModel
{
    /// <summary>
    /// Gets or sets the namespace for the Razor Page.
    /// </summary>
    public string? RazorPageNamespace { get; set; }

    /// <summary>
    /// Gets the correct CSS class for an input element based on the .NET type name.
    /// </summary>
    /// <param name="inputType">The .NET type name.</param>
    /// <returns>The CSS class for the input element.</returns>
    public string GetInputClassType(string inputType)
    {
        return string.Equals(inputType, "bool", StringComparison.OrdinalIgnoreCase) ?
            "form-check-input" : "form-control";
    }

    /// <summary>
    /// Gets the correct HTML tag for an input element based on the .NET type name.
    /// </summary>
    /// <param name="inputType">The .NET type name.</param>
    /// <returns>The HTML tag for the input element.</returns>
    public string GetInputTagType(string inputType)
    {
        //default tag being <input>
        string inputTag = "input";
        var lowerInputType = inputType.ToLower();
        //using a switch case since this will be expanded very soon
        switch (inputType)
        {
            case "enum":
            case "system.enum":
                inputTag = "select";
                break;
        }

        return inputTag;
    }
}
