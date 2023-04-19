// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
