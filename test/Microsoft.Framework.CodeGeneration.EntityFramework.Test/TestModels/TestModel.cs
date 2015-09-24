// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Conventions.Internal;

namespace Microsoft.Framework.CodeGeneration.EntityFramework.Test.TestModels
{
    public static class TestModel
    {
        public static IModel Model
        {
            get
            {
                var builder = new ModelBuilder(new CoreConventionSetBuilder().CreateConventionSet());

                builder.Entity<Product>()
                    .HasOne(p => p.ProductCategory)
                    .WithMany(c => c.CategoryProducts)
                    .ForeignKey(e => e.ProductCategoryId);

                return builder.Model;
            }
        }
    }
}