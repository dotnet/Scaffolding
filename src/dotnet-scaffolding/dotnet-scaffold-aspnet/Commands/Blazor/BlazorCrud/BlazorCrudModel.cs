// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;

internal class BlazorCrudModel
{
    public required string PageType { get; init; }
    public required DbContextInfo DbContextInfo { get; init; }
    public required ModelInfo ModelInfo { get; init; }
    public required ProjectInfo ProjectInfo { get; init; }

    //used to get correct Input tag to add to BlazorCrud\Create.tt and BlazorCrud\Edit.tt template
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
            case "System.DateTime":
            case "System.DateTimeOffset":
            case "System.DateOnly":
            case "System.TimeOnly":
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

    //used to get correct form class to add to BlazorCrud\Create.tt and BlazorCrud\Edit.tt template
    public string GetInputClassType(string inputType)
    {
        if (string.Equals(inputType, "bool", StringComparison.OrdinalIgnoreCase))
        {
            return "form-check-input";
        }

        return "form-control";
    }
}
