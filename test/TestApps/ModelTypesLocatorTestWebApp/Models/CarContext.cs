using Microsoft.EntityFrameworkCore;
using ModelTypesLocatorTestClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelTypesLocatorTestWebApp.Models
{
    public class CarContext : DbContext
    {
        public CarContext(DbContextOptions<CarContext> options)
            : base(options)
        {
        }

        public DbSet<Car> Car { get; set; }
        public DbSet<Manufacturer> Manufacturer { get; set; }
    }
}
