using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace TestWebApp.Models
{
    public class Person
    {
        public int PersonId { get; set; }

        public string Name { get; set; }
    }

    //Intentionally kept in this file rather than separate file for testing non-normal case.
    public class PersonContext : DbContext
    {
        public DbSet<Person> People { get; set; }

        protected override void OnConfiguring(DbContextOptions options)
        {
            options.UseSqlServer(@"Data Source=.\SQLEXPRESS;Initial Catalog=PersonContext;Integrated Security=True;MultipleActiveResultSets=True");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Person>().Key(a => a.PersonId);
        }
    }
}