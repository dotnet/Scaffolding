// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CodeChangeType
{
    Default,
    MemberAccess,
    Lambda
}

