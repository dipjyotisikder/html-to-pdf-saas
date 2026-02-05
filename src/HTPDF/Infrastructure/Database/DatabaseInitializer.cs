using HTPDF.Infrastructure.Database.Entities;
using HTPDF.Infrastructure.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HTPDF.Infrastructure.Database;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager, services);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILoggingService<Program>>();
            logger.LogError(ex, LogMessages.Infrastructure.DatabaseMigrationError);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, IServiceProvider services)
    {
        var adminEmail = "admin@htmltopdf.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                var programLogger = services.GetRequiredService<ILoggingService<Program>>();
                programLogger.LogInfo(LogMessages.Infrastructure.AdminUserCreated, adminEmail);
            }
        }
    }
}
