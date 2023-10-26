// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class PropertyMetadata : IPropertyMetadata
    {
        internal PropertyMetadata()
        {
        }

        /// <summary>
        /// Use this constructor when the model is being used without datacontext.
        /// It will set the property as:
        ///   Non primary
        ///   Non foreign key.
        ///   Non autogenerated
        ///   Writable
        ///   Non Enum type
        /// </summary>
        /// <param name="property"></param>
        public PropertyMetadata(PropertyInfo property)
        {
            Contract.Assert(property != null && property.Name != null && property.PropertyType != null);
            PropertyInfo = property;
            PropertyName = property.Name;
            TypeName = property.PropertyType.FullName;

            ShortTypeName = TypeUtil.GetShortTypeName(property.PropertyType);
            Scaffold = true;
            var scaffoldAttr = property.GetCustomAttribute(typeof(ScaffoldColumnAttribute)) as ScaffoldColumnAttribute;
            if (scaffoldAttr != null && !scaffoldAttr.Scaffold)
            {
                Scaffold = false;
            }

            var dataTypeAttribute = property.GetCustomAttribute(typeof(DataTypeAttribute)) as DataTypeAttribute;
            IsMultilineText = (dataTypeAttribute != null) && (dataTypeAttribute.DataType == DataType.MultilineText);

            // Since this is not being treated as an EF based model,
            // below values are set as false.
            IsPrimaryKey = false;
            IsForeignKey = false;
            IsEnum = false;
            IsEnumFlags = false;
            IsReadOnly = false;
            IsAutoGenerated = false;
        }

        public bool IsAutoGenerated { get; set; }

        public bool IsEnum { get; set; }

        public bool IsEnumFlags { get; set; }

        public bool IsForeignKey { get; set; }

        public bool IsPrimaryKey { get; set; }

        public bool IsReadOnly { get; set; }

        public string PropertyName { get; set; }

        public bool Scaffold { get; set; }

        public string ShortTypeName { get; set; }

        public string TypeName { get; set; }

        public bool IsMultilineText { get; set; }

        public PropertyInfo PropertyInfo { get; set; }
    }

    public class PropertyMetadataEqualityComparer : IEqualityComparer<IPropertyMetadata>
    {
        public bool Equals(IPropertyMetadata x, IPropertyMetadata y)
        {
            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.PropertyName, y.PropertyName) &&
                string.Equals(x.ShortTypeName, y.ShortTypeName) &&
                string.Equals(x.TypeName, y.TypeName);
        }
        
        public int GetHashCode(IPropertyMetadata obj)
        {
            var hash = obj.GetHashCode();
            return hash;
        }
    }
}
