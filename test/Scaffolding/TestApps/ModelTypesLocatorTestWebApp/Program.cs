using Microsoft.EntityFrameworkCore;
using ModelTypesLocatorTestWebApp.Models;

namespace ModelTypesLocatorTestWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            builder.Services.AddDbContext<CarContext>(options =>
                options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=CarContext-3ae1a974-3117-4483-853a-06d90fdb3bd0;Trusted_Connection=True;MultipleActiveResultSets=true"));

            var app = builder.Build();

            app.Run();
        }
    }
}
