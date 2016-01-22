// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Conventions.Internal;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework.Test.TestModels
{
    public static class TestModel
    {
        public static IModel CategoryProductModel
        {
            get
            {
                var builder = new ModelBuilder(new CoreConventionSetBuilder().CreateConventionSet());

                builder.Entity<Product>();
                builder.Entity<Category>();

                return builder.Model;
            }
        }

        public static IModel CustomerOrderModel
        {
            get
            {
                var builder = new ModelBuilder(new CoreConventionSetBuilder().CreateConventionSet());

                builder.Entity<Customer>();
                builder.Entity<Order>();

                return builder.Model;
            }
        }
    }
}