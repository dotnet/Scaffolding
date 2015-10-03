// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework.Test.TestModels
{
    public class TestDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
    }
}