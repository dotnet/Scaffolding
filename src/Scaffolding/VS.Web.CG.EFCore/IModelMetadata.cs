// Copyright (c) .NET Foundation. All rights reserved.

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
