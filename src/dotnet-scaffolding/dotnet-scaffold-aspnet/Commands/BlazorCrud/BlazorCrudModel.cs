// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.BlazorCrud;

internal class BlazorCrudModel
{
    public required string PageType { get; init; }
    public required DbContextInfo DbContextInfo { get; init; }
    public required ModelInfo ModelInfo { get; init; }
    public required ProjectInfo ProjectInfo { get; init; }

    public string GetInputType(string inputType)
    {
        if (string.IsNullOrEmpty(inputType))
        {
            return "InputText";
        }

        switch (inputType)
        {
            case "string":
                return "InputText";
            case "DateTime":
            case "DateTimeOffset":
            case "DateOnly":
            case "TimeOnly":
                return "InputDate";
            case "int":
            case "long":
            case "short":
            case "float":
            case "decimal":
            case "double":
                return "InputNumber";
            case "bool":
                return "InputCheckbox";
            case "enum":
            case "enum[]":
                return "InputSelect";
            default:
                return "InputText";
        }
    }

    //used for Create and Edit pages for the Blazor CRUD scenario
    public string GetInputClassType(string inputType)
    {
        if (string.Equals(inputType, "bool", StringComparison.OrdinalIgnoreCase))
        {
            return "form-check-input";
        }

        return "form-control";
    }
}
