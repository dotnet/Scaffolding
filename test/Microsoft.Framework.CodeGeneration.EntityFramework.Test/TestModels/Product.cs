// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.CodeGeneration.EntityFramework.Test.TestModels
{
    public class Product
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int ProductCategoryId { get; set; }

        public Category ProductCategory { get; set; }

        public EnumType ProductEnumProperty { get; set; }
    }
}