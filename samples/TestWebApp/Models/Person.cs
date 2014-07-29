using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace TestWebApp.Models
{
    public class Person
    {
        public int PersonId { get; set; }

        public string Name { get; set; }

        public bool BoolProperty { get; set; }
    }

    //Intentionally kept in this file rather than separate file for testing non-normal case.
    public class PersonContext : DbContext
    {
        private static bool isCreated = false;

        public PersonContext()
        {
            if (!isCreated)
            {
                this.Database.EnsureCreated();
                isCreated = true;
            }
        }

        //public DbSet<Person> People { get; set; }

        protected override void OnConfiguring(DbContextOptions options)
        {
            options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=aspnetvnext-TestWebApp-6a883536-855a-4c46-84f6-09412e2735c9;Trusted_Connection=True;MultipleActiveResultSets=true");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Person>();
        }
    }
}