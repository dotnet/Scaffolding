// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.CodeGeneration.EntityFrameworkCore.Test.TestModels
{
    /*
        Category  --(one)----(many)-->   Product  [foreign key on Product] [Read from right to left for many to one relation :-)]
        Customer  --(zero)---(one)--->   CustomerDetails [Independent Association]
        Order --has---> Customer [Foreign key relation]
    */

    public class Product
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int CategoryId { get; set; }

        public Category Category { get; set; } // Navigation

        public EnumType EnumProperty { get; set; }

        public EnumFlagsType EnumFlagsProperty { get; set; }

        [ScaffoldColumn(true)]
        public string ExplicitScaffoldProperty { get; set; }

        [ScaffoldColumn(false)]
        public int ScaffoldFalseProperty { get; set; }

        //[ReadOnly(true)]
        public string ReadOnlyProperty { get; set; }
    }

    public class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        public ICollection<Product> Products { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; }

        public CustomerDetails CustomerDetails { get; set; } //Independent Association
    }

    public class CustomerDetails
    {
        public int CustomerDetailsId { get; set; }

        public string Details { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }

        public string OrderName { get; set; }

        public int CustomerId { get; set; }

        public Customer Customer { get; set; }
    }

    public enum EnumType
    {
        Value1,
        Value2
    }

    [Flags]
    public enum EnumFlagsType
    {
        Flag1 = 1,
        Flag2 = 2
    }
}
