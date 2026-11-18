// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Describes a reference navigation on a model that is paired with a single scalar foreign key property.
/// </summary>
internal class ForeignKeyNavigationInfo
{
    /// <summary>
    /// Gets the name of the reference navigation property (e.g. "Country").
    /// </summary>
    public required string NavigationPropertyName { get; init; }
    /// <summary>
    /// Gets the name of the scalar foreign key property (e.g. "CountryId").
    /// </summary>
    public required string ForeignKeyPropertyName { get; init; }
    /// <summary>
    /// Gets the primary key property name of the principal entity.
    /// </summary>
    public required string PrincipalPrimaryKeyName { get; init; }
    /// <summary>
    /// Gets the principal entity property used as the display text of a select list.
    /// </summary>
    public required string DisplayPropertyName { get; init; }
    /// <summary>
    /// Gets the DbContext member expression for the principal entity set (e.g. "Countries" or "Set&lt;Models.Country&gt;()").
    /// </summary>
    public required string EntitySetName { get; init; }
}
