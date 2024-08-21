// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

internal class ViewModel : CrudModel
{
    public string GetInputClassType(string inputType)
    {
        return string.Equals(inputType, "bool", StringComparison.OrdinalIgnoreCase) ?
            "form-check-input" : "form-control";
    }

    public string GetInputTagType(string inputType)
    {
        //default tag being <input>
        string inputTag = "input";
        var lowerInputType = inputType.ToLower();
        //using a switch case since this will be expanded very soon
        switch(inputType)
        {
            case "enum":
            case "system.enum":
                inputTag = "select";
                break;
        }

        return inputTag;
    }
}
