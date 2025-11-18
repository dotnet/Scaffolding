// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

/// <summary>
/// Specifies the types of interactive pickers available for parameter selection in the UI.
/// </summary>
public enum InteractivePickerType
{
    None,
    ClassPicker,
    FilePicker,
    ProjectPicker,
    CustomPicker,
    YesNo,
    ConditionalPicker
}
