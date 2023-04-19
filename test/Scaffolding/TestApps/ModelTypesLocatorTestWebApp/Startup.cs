// Copyright (c) .NET Foundation. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelTypesLocatorTestWebApp.Models;

namespace ModelTypesLocatorTestWebApp
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<CarContext>(options =>
                    options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=CarContext-3ae1a974-3117-4483-853a-06d90fdb3bd0;Trusted_Connection=True;MultipleActiveResultSets=true"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
        }
    }
}
