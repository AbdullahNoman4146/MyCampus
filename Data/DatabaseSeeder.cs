using Microsoft.EntityFrameworkCore;
using MyCampus.Services;

namespace MyCampus.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                
                // Ensure migrations are applied
                await context.Database.MigrateAsync();

                // Seed official hackathon repo data if not yet imported
                var importService = services.GetRequiredService<IJsonImportService>();
                if (!await context.RoomBookings.AnyAsync(b => b.BookedBy == "Nusrat Jahan"))
                {
                    logger.LogInformation("Importing official hackathon JSON resources from GitHub repo into SQL Server...");
                    await importService.ImportHackathonResourcesAsync(clearExisting: true);
                }
                else
                {
                    await importService.SeedAllAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the SQL Server database.");
            }
        }
    }
}
