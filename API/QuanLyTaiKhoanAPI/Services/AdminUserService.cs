using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.DTOs;
using QuanLyTaiKhoanAPI.DTOs.Audit;
using QuanLyTaiKhoanAPI.DTOs.Admin;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Interfaces;
using QuanLyTaiKhoanAPI.Middlewares;

namespace QuanLyTaiKhoanAPI.Services;

public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogService _auditLogService;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext context,
        IPermissionService permissionService,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _permissionService = permissionService;
        _auditLogService = auditLogService;
    }

    public async Task<IEnumerable<object>> GetAllUsersAsync()
    {
        var users = await _userManager.Users
            .Include(user => user.UserPermissions)
            .ThenInclude(userPermission => userPermission.Permission)
            .OrderBy(user => user.UserName)
            .ToListAsync();

        var result = new List<object>();

        foreach (var user in users)
        {
            result.Add(await MapUserAsync(user));
        }

        return result;
    }

    public async Task<object?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.Users
            .Include(item => item.UserPermissions)
            .ThenInclude(userPermission => userPermission.Permission)
            .FirstOrDefaultAsync(item => item.Id == id);

        return user == null ? null : await MapUserAsync(user);
    }

    public async Task<object> CreateUserAsync(RegisterDto dto)
    {
        var email = NormalizeRequired(dto.Email, "Email is required");
        var password = NormalizeRequired(dto.Password, "Password is required");
        var fullName = NormalizeRequired(dto.FullName, "FullName is required");
        var userName = string.IsNullOrWhiteSpace(dto.UserName) ? email : dto.UserName.Trim();

        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            FullName = fullName,
            PhoneNumber = NormalizeNullable(dto.PhoneNumber),
            Address = NormalizeNullable(dto.Address),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new BadRequestException(JoinIdentityErrors(result));
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "UserCreated",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "User",
            EntityId = user.Id,
            Details = $"Email={user.Email}"
        });

        return await GetUserByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Cannot load created user.");
    }

    public async Task UpdateUserAsync(string id, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new NotFoundException("User not found");

        var changes = new List<string>();

        if (dto.FullName != null)
        {
            user.FullName = dto.FullName.Trim();
            changes.Add("FullName");
        }

        if (dto.Address != null)
        {
            user.Address = NormalizeNullable(dto.Address);
            changes.Add("Address");
        }

        if (dto.PhoneNumber != null)
        {
            user.PhoneNumber = NormalizeNullable(dto.PhoneNumber);
            changes.Add("PhoneNumber");
        }

        if (dto.IsActive.HasValue)
        {
            user.IsActive = dto.IsActive.Value;
            changes.Add(dto.IsActive.Value ? "Activated" : "Deactivated");
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new BadRequestException(JoinIdentityErrors(result));
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "UserUpdated",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "User",
            EntityId = user.Id,
            Details = changes.Count == 0 ? "No tracked fields changed" : string.Join(", ", changes)
        });
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found");

        var role = await GetRoleByNameOrThrowAsync(roleName);
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(role.Name!, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            throw new BadRequestException(JoinIdentityErrors(result));
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "UserRoleAssigned",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "Role",
            EntityId = role.Id,
            Details = $"Role={role.Name}"
        });
    }

    public async Task RemoveRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found");

        var role = await GetRoleByNameOrThrowAsync(roleName);
        var isInRole = await _userManager.IsInRoleAsync(user, role.Name!);
        if (!isInRole)
        {
            return;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            throw new BadRequestException(JoinIdentityErrors(result));
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "UserRoleRemoved",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "Role",
            EntityId = role.Id,
            Details = $"Role={role.Name}"
        });
    }

    public async Task<IEnumerable<object>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync();

        var roleIds = roles.Select(role => role.Id).ToList();
        var rolePermissions = await _context.RolePermissions
            .Where(rolePermission => roleIds.Contains(rolePermission.RoleId))
            .Include(rolePermission => rolePermission.Permission)
            .ToListAsync();

        var userRoles = await _context.UserRoles
            .Where(userRole => roleIds.Contains(userRole.RoleId))
            .ToListAsync();

        return roles.Select(role => new
        {
            role.Id,
            RoleName = role.Name,
            Permissions = rolePermissions
                .Where(rolePermission => rolePermission.RoleId == role.Id && rolePermission.Permission != null)
                .Select(rolePermission => rolePermission.Permission!.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList(),
            UserCount = userRoles.Count(userRole => userRole.RoleId == role.Id)
        });
    }

    public async Task<object?> GetRoleByIdAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return null;
        }

        return await MapRoleAsync(role);
    }

    public async Task<object> CreateRoleAsync(string roleName)
    {
        var normalizedRoleName = NormalizeRequired(roleName, "RoleName is required");
        if (await _roleManager.RoleExistsAsync(normalizedRoleName))
        {
            throw new BadRequestException("Role already exists");
        }

        var role = new IdentityRole(normalizedRoleName);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new BadRequestException(JoinIdentityErrors(result));
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "RoleCreated",
            EntityName = "Role",
            EntityId = role.Id,
            Details = $"Role={role.Name}"
        });

        return await MapRoleAsync(role);
    }

    public async Task UpdateRoleAsync(string roleId, string roleName)
    {
        var normalizedRoleName = NormalizeRequired(roleName, "RoleName is required");
        var role = await _roleManager.FindByIdAsync(roleId)
            ?? throw new NotFoundException("Role not found");

        var existingRole = await _roleManager.FindByNameAsync(normalizedRoleName);
        if (existingRole != null && existingRole.Id != roleId)
        {
            throw new BadRequestException("Role already exists");
        }

        role.Name = normalizedRoleName;
        role.NormalizedName = _roleManager.NormalizeKey(normalizedRoleName);

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            throw new BadRequestException(JoinIdentityErrors(result));
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "RoleUpdated",
            EntityName = "Role",
            EntityId = role.Id,
            Details = $"Role={role.Name}"
        });
    }

    public async Task DeleteRoleByIdAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId)
            ?? throw new NotFoundException("Role not found");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            throw new BadRequestException(JoinIdentityErrors(result));
        }

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "RoleDeleted",
            EntityName = "Role",
            EntityId = roleId,
            Details = $"Role={role.Name}"
        });
    }

    public async Task<IEnumerable<string>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Name)
            .Select(permission => permission.Name)
            .ToListAsync();
    }

    public async Task AssignPermissionAsync(string userId, string permissionName)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found");

        var permission = await GetPermissionByNameOrThrowAsync(permissionName);
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(item => item.UserId == userId && item.PermissionId == permission.Id);

        if (userPermission == null)
        {
            _context.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permission.Id,
                IsGranted = true
            });
        }
        else
        {
            userPermission.IsGranted = true;
        }

        await _context.SaveChangesAsync();

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "UserPermissionGranted",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "Permission",
            EntityId = permission.Id.ToString(),
            Details = $"Permission={permission.Name}"
        });
    }

    public async Task RemovePermissionAsync(string userId, string permissionName)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found");

        var permission = await GetPermissionByNameOrThrowAsync(permissionName);
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(item => item.UserId == userId && item.PermissionId == permission.Id);

        if (userPermission == null)
        {
            _context.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permission.Id,
                IsGranted = false
            });
        }
        else
        {
            userPermission.IsGranted = false;
        }

        await _context.SaveChangesAsync();

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "UserPermissionDenied",
            TargetUserId = user.Id,
            TargetUserName = user.UserName,
            EntityName = "Permission",
            EntityId = permission.Id.ToString(),
            Details = $"Permission={permission.Name}"
        });
    }

    public async Task AssignPermissionToRoleAsync(string roleId, string permissionName)
    {
        var role = await _roleManager.FindByIdAsync(roleId)
            ?? throw new NotFoundException("Role not found");

        var permission = await GetPermissionByNameOrThrowAsync(permissionName);
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(item => item.RoleId == roleId && item.PermissionId == permission.Id);

        if (rolePermission == null)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });

            await _context.SaveChangesAsync();

            await _auditLogService.WriteAsync(new AuditLogEntry
            {
                Action = "RolePermissionAssigned",
                EntityName = "Role",
                EntityId = role.Id,
                Details = $"Role={role.Name}; Permission={permission.Name}"
            });
        }
    }

    public async Task RemovePermissionFromRoleAsync(string roleId, string permissionName)
    {
        var role = await _roleManager.FindByIdAsync(roleId)
            ?? throw new NotFoundException("Role not found");

        var permission = await GetPermissionByNameOrThrowAsync(permissionName);
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(item => item.RoleId == roleId && item.PermissionId == permission.Id);

        if (rolePermission == null)
        {
            return;
        }

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync();

        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "RolePermissionRemoved",
            EntityName = "Role",
            EntityId = role.Id,
            Details = $"Role={role.Name}; Permission={permission.Name}"
        });
    }

    private async Task<object> MapUserAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var rolePermissions = await _context.RolePermissions
            .Where(rolePermission => rolePermission.Role != null
                && rolePermission.Permission != null
                && roles.Contains(rolePermission.Role.Name!))
            .Select(rolePermission => rolePermission.Permission!.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        var directPermissions = user.UserPermissions
            .Where(userPermission => userPermission.IsGranted && userPermission.Permission != null)
            .Select(userPermission => userPermission.Permission!.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        var effectivePermissions = await _permissionService.GetPermissionsAsync(user);

        return new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.Address,
            user.PhoneNumber,
            user.IsActive,
            Roles = roles.OrderBy(role => role).ToList(),
            Permissions = directPermissions,
            RolePermissions = rolePermissions,
            EffectivePermissions = effectivePermissions.OrderBy(permission => permission).ToList()
        };
    }

    private async Task<object> MapRoleAsync(IdentityRole role)
    {
        var permissions = await _context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == role.Id)
            .Include(rolePermission => rolePermission.Permission)
            .Where(rolePermission => rolePermission.Permission != null)
            .Select(rolePermission => rolePermission.Permission!.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        var userCount = await _context.UserRoles.CountAsync(userRole => userRole.RoleId == role.Id);

        return new
        {
            role.Id,
            RoleName = role.Name,
            Permissions = permissions,
            UserCount = userCount
        };
    }

    private async Task<IdentityRole> GetRoleByNameOrThrowAsync(string roleName)
    {
        var normalizedRoleName = NormalizeRequired(roleName, "RoleName is required");
        return await _roleManager.FindByNameAsync(normalizedRoleName)
            ?? throw new NotFoundException("Role not found");
    }

    private async Task<Permission> GetPermissionByNameOrThrowAsync(string permissionName)
    {
        var normalizedPermissionName = NormalizeRequired(permissionName, "PermissionName is required");
        return await _context.Permissions.FirstOrDefaultAsync(permission => permission.Name == normalizedPermissionName)
            ?? throw new NotFoundException("Permission not found");
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BadRequestException(errorMessage);
        }

        return value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string JoinIdentityErrors(IdentityResult result)
    {
        return string.Join(", ", result.Errors.Select(error => error.Description));
    }
}
