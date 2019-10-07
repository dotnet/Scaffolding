// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;

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

                return builder.Model;
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

                return builder.Model;
            }
        }
    }
}
