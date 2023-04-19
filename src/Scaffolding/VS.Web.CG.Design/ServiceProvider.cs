// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if NET461
using System.ComponentModel;
#endif
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    internal class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly IServiceProvider _fallbackServiceProvider;

        public ServiceProvider()
        {
            _instances[typeof(IServiceProvider)] = this;
        }

        public ServiceProvider(IServiceProvider fallbackServiceProvider)
            : this()
        {
            _fallbackServiceProvider = fallbackServiceProvider;
        }

        public void Add(Type type, object instance)
        {
            _instances[type] = instance;
        }

        public void Add<T>(object instance)
        {
            _instances[typeof(T)] = instance;
        }

        public object GetService(Type serviceType)
        {
            object instance;
            if (_instances.TryGetValue(serviceType, out instance))
            {
                return instance;
            }

            if (_fallbackServiceProvider != null)
            {
                return _fallbackServiceProvider.GetService(serviceType);
            }

            return null;
        }

        internal void AddServiceWithDependencies<TService, TImplementation>()
        {
            Add(typeof(TService), ActivatorUtilities.CreateInstance<TImplementation>(this));
        }
    }
}
