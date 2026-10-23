using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<EmptyWebApp.Data.ApplicationUser>(options)
{
}
