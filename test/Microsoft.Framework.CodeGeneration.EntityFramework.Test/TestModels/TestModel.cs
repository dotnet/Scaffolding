// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Metadata;

namespace Microsoft.Framework.CodeGeneration.EntityFramework.Test.TestModels
{
    public static class TestModel
    {
        private static Model _model;

        public static IModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = new Model();
                    var builder = new ModelBuilder(_model);

                    builder.Entity<Product>();
                    builder.Entity<Category>();

                    var categoryType = _model.GetEntityType(typeof(Category));
                    var productType = _model.GetEntityType(typeof(Product));

                    var categoryFk = productType.AddForeignKey(categoryType.GetKey(), productType.GetProperty("ProductCategoryId"));

                    categoryType.AddNavigation(new Navigation(categoryFk, "CategoryProducts", pointsToPrincipal: false));
                    productType.AddNavigation(new Navigation(categoryFk, "ProductCategory", pointsToPrincipal: true));
                }
                return _model;
            }
        }
    }
}