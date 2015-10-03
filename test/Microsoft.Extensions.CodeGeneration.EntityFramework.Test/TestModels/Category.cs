// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework.Test.TestModels
{
    public class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        public ICollection<Product> CategoryProducts { get; set; }
    }
}