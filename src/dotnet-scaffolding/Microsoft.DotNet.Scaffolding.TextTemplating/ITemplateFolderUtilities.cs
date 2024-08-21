// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating;

internal interface ITemplateFolderService
{
    IEnumerable<string> GetTemplateFolders(string[] baseFolders);
    IEnumerable<string> GetAllT4Templates(string[] baseFolders);
    IEnumerable<string> GetAllFiles(string[] baseFolders, string extension);
}
