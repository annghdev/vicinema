using System.Security.Claims;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.WebServer.Controllers;

/// <summary>
/// Handles administrative roles and static permission policy modifications.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application", Roles = "SystemAdmin,Admin")]
[EnableRateLimiting("fixed")]
public class RoleController : Controller
{
    private readonly RoleManager<Role> _roleManager;

    public RoleController(RoleManager<Role> roleManager)
    {
        _roleManager = roleManager;
    }

    /// <summary>
    /// Displays roles and associated permission settings.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý phân quyền";

        var roles = await _roleManager.Roles.ToListAsync();
        roles = roles
            .OrderByDescending(r => r.Name == RoleNames.SystemAdmin)
            .ThenBy(r => r.Name == RoleNames.Customer)
            .ThenBy(r => r.Name)
            .ToList();
        var rolePermissions = new Dictionary<Guid, List<string>>();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == AuthClaimTypes.Permission)
                .Select(c => c.Value)
                .ToList();

            rolePermissions[role.Id] = permissions;
        }

        var availablePermissions = typeof(Permissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(fi => (string)fi.GetValue(null)!)
            .ToList();

        var model = new RoleManagementViewModel
        {
            Roles = roles,
            RolePermissions = rolePermissions,
            AvailablePermissions = availablePermissions
        };

        return View(model);
    }

    /// <summary>
    /// Updates permission mappings for a selected target role safely.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePermissions(Guid roleId, List<string>? permissions)
    {
        permissions ??= [];
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
        {
            TempData["Error"] = "Không tìm thấy vai trò.";
            return RedirectToAction(nameof(Index));
        }

        bool isSysAdmin = User.IsInRole(RoleNames.SystemAdmin);
        bool isAdmin = User.IsInRole(RoleNames.Admin);

        if (!isSysAdmin && !isAdmin)
        {
            TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
            return RedirectToAction(nameof(Index));
        }

        if (isAdmin && !isSysAdmin)
        {
            if (role.Name == RoleNames.SystemAdmin || role.Name == RoleNames.Admin)
            {
                TempData["Error"] = "Admin không thể sửa phân quyền của System Admin hoặc Admin.";
                return RedirectToAction(nameof(Index));
            }
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == AuthClaimTypes.Permission)
            .ToList();

        // 1. Remove unchecked
        foreach (var claim in existingPermissions)
        {
            if (!permissions.Contains(claim.Value))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
        }

        // 2. Add checked
        foreach (var perm in permissions)
        {
            if (!existingPermissions.Any(c => c.Value == perm))
            {
                await _roleManager.AddClaimAsync(role, new Claim(AuthClaimTypes.Permission, perm));
            }
        }

        TempData["Success"] = $"Cập nhật phân quyền cho vai trò {role.Name} thành công.";
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>
/// ViewModel mapping roles matrix securely.
/// </summary>
[EnableRateLimiting("fixed")]
public class RoleManagementViewModel
{
    public List<Role> Roles { get; set; } = [];
    public Dictionary<Guid, List<string>> RolePermissions { get; set; } = [];
    public List<string> AvailablePermissions { get; set; } = [];
}

