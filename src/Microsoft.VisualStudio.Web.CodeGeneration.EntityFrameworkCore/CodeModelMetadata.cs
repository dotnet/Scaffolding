// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    /// <summary>
    /// CodeModelMetadata is used to expose properties of a model
    /// without Entity type information
    /// </summary>
    public class CodeModelMetadata : IModelMetadata
    {
        private IPropertyMetadata[] _properties;
        private NavigationMetadata[] _navigations;
        private IPropertyMetadata[] _primaryKeys;
        private Type _model;

        private static Type[] bindableNonPrimitiveTypes = new Type[]
        {
            typeof(string),
            typeof(decimal),
            typeof(Guid),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan)
        };

        public CodeModelMetadata(Type model)
        {
            if(model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            _model = model;
            _navigations = new NavigationMetadata[0];
            _primaryKeys = new IPropertyMetadata[0];
        }

        /// <summary>
        /// Always returns empty array as there is no Entity type information
        /// </summary>
        public INavigationMetadata[] Navigations
        {
            get
            {
                return _navigations;
            }
        }

        /// <summary>
        /// Always return empty array as there is no Entity type information
        /// </summary>
        public IPropertyMetadata[] PrimaryKeys
        {
            get
            {
                return _primaryKeys;
            }
        }

        /// <summary>
        /// Returns an array of properties that are bindable.
        /// (For eg. primitive types, strings, DateTime etc.)
        /// </summary>
        public IPropertyMetadata[] Properties
        {
            get
            {
                if(_properties == null)
                {
                    _properties = GetBindableProperties(_model);
                }
                return _properties;
            }
        }

        private IPropertyMetadata[] GetBindableProperties(Type _model)
        {
            var props = _model.GetProperties().Where(p => IsBindable(p));
            return props.Select(p => new PropertyMetadata(p)).ToArray();
        }

        private bool IsBindable(PropertyInfo propertyInfo)
        {
            if(propertyInfo == null)
            {
                return false;
            }

            if(TypeUtilities.IsTypePrimitive(propertyInfo.PropertyType) || bindableNonPrimitiveTypes.Any(bindable => bindable == propertyInfo.PropertyType))
            {
                return true;
            }
            return false;
        }
    }
}
