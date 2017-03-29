// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public interface INavigationMetadata
    {
        string AssociationPropertyName { get; set; }
        string DisplayPropertyName { get; set; }
        string EntitySetName { get; set; }
        string[] ForeignKeyPropertyNames { get; set; }
        string[] PrimaryKeyNames { get; set; }
        string ShortTypeName { get; set; }
        string TypeName { get; set; }
    }
}