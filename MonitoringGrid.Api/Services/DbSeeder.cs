using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.Services;

public interface IDbSeeder
{
    Task SeedAsync();
}

public class DbSeeder : IDbSeeder
{
    private readonly MonitoringContext _context;
    private readonly ILogger<DbSeeder> _logger;
    private readonly IEncryptionService _encryptionService;

    public DbSeeder(MonitoringContext context, ILogger<DbSeeder> logger, IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if data already exists
            if (_context.Users.Any())
            {
                _logger.LogInformation("Database already seeded");
                return;
            }

            _logger.LogInformation("Seeding in-memory database with test data...");

            // Create test user
            var adminUserId = Guid.NewGuid().ToString();
            var adminUser = new User
            {
                UserId = adminUserId,
                Username = "admin",
                Email = "admin@test.com",
                DisplayName = "Admin User",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                PasswordHash = _encryptionService.Hash("admin123") // Set password directly on user
            };

            _context.Users.Add(adminUser);

            // Create password history entry
            var userPassword = new UserPassword
            {
                UserId = adminUserId,
                PasswordHash = _encryptionService.Hash("admin123"),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserPasswords.Add(userPassword);

            // Create admin role
            var adminRoleId = Guid.NewGuid().ToString();
            var adminRole = new Role
            {
                RoleId = adminRoleId,
                Name = "Admin",
                Description = "Administrator role with full access",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Roles.Add(adminRole);

            // Assign admin role to admin user
            var userRole = new UserRole
            {
                UserId = adminUserId,
                RoleId = adminRoleId,
                AssignedDate = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);

            // Create some basic permissions
            var permissions = new[]
            {
                new Permission { PermissionId = Guid.NewGuid().ToString(), Name = "ViewKPIs", Description = "View KPIs", Resource = "KPI", Action = "View", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
                new Permission { PermissionId = Guid.NewGuid().ToString(), Name = "ManageKPIs", Description = "Manage KPIs", Resource = "KPI", Action = "Manage", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
                new Permission { PermissionId = Guid.NewGuid().ToString(), Name = "ViewAlerts", Description = "View Alerts", Resource = "Alert", Action = "View", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
                new Permission { PermissionId = Guid.NewGuid().ToString(), Name = "ManageAlerts", Description = "Manage Alerts", Resource = "Alert", Action = "Manage", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
                new Permission { PermissionId = Guid.NewGuid().ToString(), Name = "ViewUsers", Description = "View Users", Resource = "User", Action = "View", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
                new Permission { PermissionId = Guid.NewGuid().ToString(), Name = "ManageUsers", Description = "Manage Users", Resource = "User", Action = "Manage", IsActive = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
            };

            _context.Permissions.AddRange(permissions);

            // Assign all permissions to admin role
            foreach (var permission in permissions)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = adminRoleId,
                    PermissionId = permission.PermissionId,
                    AssignedDate = DateTime.UtcNow
                };
                _context.RolePermissions.Add(rolePermission);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeded successfully with test user: admin/admin123");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }


}
