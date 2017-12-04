// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test.TestModels
{
    public static class TestModel
    {
        public class TestServiceFactory
        {
            public static readonly TestServiceFactory Instance = new TestServiceFactory();

            private TestServiceFactory()
            {
            }

            private readonly ConcurrentDictionary<Type, IServiceProvider> _factories
                = new ConcurrentDictionary<Type, IServiceProvider>();

            public TService Create<TService>()
                where TService : class
            {
                return _factories.GetOrAdd(
                    typeof(TService),
                    t => AddType(new ServiceCollection(), typeof(TService)).BuildServiceProvider()).GetService<TService>();
            }

            private static ServiceCollection AddType(ServiceCollection serviceCollection, Type serviceType)
            {
                serviceCollection.AddSingleton(serviceType);

                var constructors = serviceType.GetConstructors();
                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException("Cannot use with no public constructors.");
                }

                var constructor = constructors[0];
                foreach (var candidate in constructors.Skip(1))
                {
                    if (candidate.GetParameters().Length > constructor.GetParameters().Length)
                    {
                        constructor = candidate;
                    }
                }

                foreach (var parameter in constructor.GetParameters())
                {
                    AddType(serviceCollection, parameter.ParameterType);
                }

                return serviceCollection;
            }
        }

        public static IModel CategoryProductModel
        {
            get
            {
                var sqlServerTypeMapper = TestServiceFactory.Instance.Create<SqlServerTypeMapper>(); 
                var builder = new ModelBuilder(new CoreConventionSetBuilder(new CoreConventionSetBuilderDependencies(sqlServerTypeMapper)).CreateConventionSet());

                builder.Entity<Product>();
                builder.Entity<Category>();

                return builder.Model;
            }
        }

        public static IModel CustomerOrderModel
        {
            get
            {
                var sqlServerTypeMapper = TestServiceFactory.Instance.Create<SqlServerTypeMapper>();
                var builder = new ModelBuilder(new CoreConventionSetBuilder(new CoreConventionSetBuilderDependencies(sqlServerTypeMapper)).CreateConventionSet());

                builder.Entity<Customer>();
                builder.Entity<Order>();

                return builder.Model;
            }
        }
    }
}