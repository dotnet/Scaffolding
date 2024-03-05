// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Helpers.Services
{
    public interface IProjectService
    {
        void Setup();
        IList<string> GetPropertyValues(string propertyName);
        IList<string> GetItemValues(string itemName);
    }
}
