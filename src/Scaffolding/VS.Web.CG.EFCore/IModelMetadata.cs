// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface IModelMetadata
    {
        IPropertyMetadata[] Properties { get; }
        IPropertyMetadata[] PrimaryKeys { get; }
        INavigationMetadata[] Navigations { get; }
        Type ModelType { get; }
    }
}
