// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels
{
    public static class TestModel
    {
        public static IModel CategoryProductModel
        {
            get
            {
                var sqlServerTypeMapper = new SqlServerTypeMapper(new RelationalTypeMapperDependencies()); 
                var builder = new ModelBuilder(new CoreConventionSetBuilder(new CoreConventionSetBuilderDependencies(sqlServerTypeMapper)).CreateConventionSet());

                builder.Entity<Product>();
                builder.Entity<Category>();

                return builder.Model;
            }
        }

        public static IModel CustomerOrderModel
        {
            get
            {
                var sqlServerTypeMapper = new SqlServerTypeMapper(new RelationalTypeMapperDependencies()); 
                var builder = new ModelBuilder(new CoreConventionSetBuilder(new CoreConventionSetBuilderDependencies(sqlServerTypeMapper)).CreateConventionSet());

                builder.Entity<Customer>();
                builder.Entity<Order>();

                return builder.Model;
            }
        }
    }
}