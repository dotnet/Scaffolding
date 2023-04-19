// Copyright (c) .NET Foundation. All rights reserved.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels
{
    public class TestDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }
    }
}
