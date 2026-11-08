using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Authorization;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.Entities;

namespace QuanLyTaiKhoanAPI.Extensions;

public static class SeedAdminPermissions
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

        const string adminRole = "Admin";
        var roleEntity = await roleManager.FindByNameAsync(adminRole);
        if (roleEntity == null)
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
            roleEntity = await roleManager.FindByNameAsync(adminRole);
        }

        if (roleEntity == null)
        {
            throw new Exception("Failed to ensure the admin role exists.");
        }

        await SeedBootstrapAdminAsync(configuration, environment, userManager, adminRole);

        var desiredPermissions = AppPermissions.All.ToHashSet(StringComparer.Ordinal);
        var existingPermissions = await context.Permissions.ToListAsync();

        var obsoletePermissions = existingPermissions
            .Where(permission => !desiredPermissions.Contains(permission.Name))
            .ToList();

        if (obsoletePermissions.Count > 0)
        {
            var obsoletePermissionIds = obsoletePermissions
                .Select(permission => permission.Id)
                .ToList();

            var obsoleteUserPermissions = context.UserPermissions
                .Where(userPermission => obsoletePermissionIds.Contains(userPermission.PermissionId));
            var obsoleteRolePermissions = context.RolePermissions
                .Where(rolePermission => obsoletePermissionIds.Contains(rolePermission.PermissionId));

            context.UserPermissions.RemoveRange(obsoleteUserPermissions);
            context.RolePermissions.RemoveRange(obsoleteRolePermissions);
            context.Permissions.RemoveRange(obsoletePermissions);
        }

        var existingPermissionNames = existingPermissions
            .Select(permission => permission.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missingPermissions = desiredPermissions
            .Where(permissionName => !existingPermissionNames.Contains(permissionName))
            .Select(permissionName => new Permission { Name = permissionName })
            .ToList();

        if (missingPermissions.Count > 0)
        {
            context.Permissions.AddRange(missingPermissions);
        }

        if (obsoletePermissions.Count > 0 || missingPermissions.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        var activePermissions = await context.Permissions.ToListAsync();
        var rolePermissions = await context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleEntity.Id)
            .ToListAsync();

        foreach (var permission in activePermissions)
        {
            if (!rolePermissions.Any(rolePermission => rolePermission.PermissionId == permission.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleEntity.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedBootstrapAdminAsync(
        IConfiguration configuration,
        IHostEnvironment environment,
        UserManager<ApplicationUser> userManager,
        string adminRole)
    {
        if (!configuration.GetValue<bool>("BootstrapAdmin:Enabled"))
        {
            return;
        }

        var userName = NormalizeBootstrapValue(configuration["BootstrapAdmin:UserName"]);
        var email = NormalizeBootstrapValue(configuration["BootstrapAdmin:Email"]);
        var password = configuration["BootstrapAdmin:Password"];
        var fullName = NormalizeBootstrapValue(configuration["BootstrapAdmin:FullName"]) ?? "Administrator";

        if (userName == null || email == null || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("BootstrapAdmin is enabled but UserName, Email, or Password is missing.");
        }

        if (environment.IsProduction() && IsUnsafeProductionPassword(password))
        {
            throw new InvalidOperationException("BootstrapAdmin:Password must be changed to a strong private value before running in production.");
        }

        var adminUser = await userManager.FindByEmailAsync(email)
            ?? await userManager.FindByNameAsync(userName);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Failed to create bootstrap admin user: " +
                    string.Join(", ", createResult.Errors.Select(error => error.Description)));
            }
        }
        else
        {
            adminUser.FullName = fullName;
            adminUser.IsActive = true;
            adminUser.EmailConfirmed = true;
            adminUser.LockoutEnabled = true;

            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException("Failed to update bootstrap admin user: " +
                    string.Join(", ", updateResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, adminRole))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, adminRole);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException("Failed to assign bootstrap admin role: " +
                    string.Join(", ", roleResult.Errors.Select(error => error.Description)));
            }
        }
    }

    private static string? NormalizeBootstrapValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsUnsafeProductionPassword(string password)
    {
        var unsafeMarkers = new[]
        {
            "CHANGE_THIS",
            "YOUR_PASSWORD",
            "PASSWORD"
        };

        return password.Length < 12 ||
            unsafeMarkers.Any(marker => password.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
