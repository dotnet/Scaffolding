// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold;

var builder = new ScaffoldCommandAppBuilder(args);
var app = builder.Build();
await app.RunAsync();
