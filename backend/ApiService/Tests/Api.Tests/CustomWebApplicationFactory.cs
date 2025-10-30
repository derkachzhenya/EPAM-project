using Epam.ItMarathon.ApiService.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Epam.ItMarathon.ApiService.Api.Tests
{
    /// <summary>
    /// Custom WebApplicationFactory for integration tests.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext using an in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using var scope = serviceProvider.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();

                // Ensure the database is created
                db.Database.EnsureCreated();
            });
        }
    }
}
