// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Metadata;

namespace Microsoft.Framework.CodeGeneration.EntityFramework.Test.TestModels
{
    public static class TestModel
    {
        public static IModel Model
        {
            get
            {
                var model = new Model();
                var builder = new ModelBuilder(model);

                builder.Entity<Product>();
                builder.Entity<Category>();

                return model;
            }
        }
    }
}