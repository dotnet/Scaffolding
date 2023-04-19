// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels
{
    public static class TestModel
    {
        public static IModel CategoryProductModel
        {
            get
            {
#pragma warning disable EF1001 // Internal EF Core API usage.
                var builder = new ModelBuilder(SqlServerConventionSetBuilder.Build());
#pragma warning restore EF1001 // Internal EF Core API usage.

                builder.Entity<Product>();
                builder.Entity<Category>();

                return (IModel)builder.Model;
            }
        }

        public static IModel CustomerOrderModel
        {
            get
            {
#pragma warning disable EF1001 // Internal EF Core API usage.
                var builder = new ModelBuilder(SqlServerConventionSetBuilder.Build());
#pragma warning restore EF1001 // Internal EF Core API usage.

                builder.Entity<Customer>();
                builder.Entity<Order>();

                return (IModel)builder.Model;
            }
        }
    }
}
