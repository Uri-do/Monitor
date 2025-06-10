using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Core.Entities;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Fixing Admin User Password Hash ===");
        
        var host = CreateHostBuilder(args).Build();
        
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Find the admin user
            var adminUser = await context.Set<User>()
                .FirstOrDefaultAsync(u => u.Username == "admin");
                
            if (adminUser == null)
            {
                Console.WriteLine("❌ Admin user not found");
                return;
            }
            
            Console.WriteLine($"✅ Found admin user: {adminUser.Username} ({adminUser.Email})");
            Console.WriteLine($"Current hash: {adminUser.PasswordHash?.Substring(0, Math.Min(50, adminUser.PasswordHash.Length))}...");
            
            // Update with correct PBKDF2 hash
            var newHash = "A0Ysoilo3DZrUMs018sk9KK0n3hnJPFGeaQO3Zpe4x0=:5OgE/8fT34r5BrAflmeSNwHDi0xGnY0iRDBzB5Es0rw=";
            var newSalt = "A0Ysoilo3DZrUMs018sk9KK0n3hnJPFGeaQO3Zpe4x0=";
            
            adminUser.PasswordHash = newHash;
            adminUser.PasswordSalt = newSalt;
            adminUser.ModifiedDate = DateTime.UtcNow;
            adminUser.ModifiedBy = "SYSTEM_PBKDF2_FIX";
            
            await context.SaveChangesAsync();
            
            Console.WriteLine("✅ Admin user password hash updated successfully");
            Console.WriteLine($"New hash: {newHash.Substring(0, 50)}...");
            Console.WriteLine("Username: admin");
            Console.WriteLine("Password: Admin123!");
            Console.WriteLine();
            Console.WriteLine("=== Fix Complete ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fixing admin user");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
    
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var connectionString = "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=Conexus2024!;TrustServerCertificate=true;";
                services.AddDbContext<MonitoringContext>(options =>
                    options.UseSqlServer(connectionString));
            });
}
