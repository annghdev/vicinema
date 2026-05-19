using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wolverine;

using Microsoft.AspNetCore.Identity;
using CinemaTicketBooking.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CinemaTicketBooking.WebServer.Controllers;

/// <summary>
/// Handles user account management for the admin portal.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class UserManagementController(IMessageBus bus, RoleManager<Role> roleManager) : Controller
{
    /// <summary>
    /// Displays a list of customer accounts.
    /// </summary>
    [Authorize(Policy = Permissions.AccountsView)]
    public async Task<IActionResult> Customers(int pageNumber = 1, int pageSize = 10, string? search = null)
    {
        ViewData["Title"] = "Quản lý khách hàng";
        
        var query = new GetPagedAccountsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = search,
            IsCustomerGroup = true
        };

        var result = await bus.InvokeAsync<PagedResult<AccountDto>>(query);
        return View(result);
    }

    /// <summary>
    /// Displays a list of system accounts (Staff, Admin, etc.).
    /// </summary>
    [Authorize(Policy = Permissions.AccountsView)]
    public async Task<IActionResult> SystemUsers(int pageNumber = 1, int pageSize = 10, string? search = null)
    {
        ViewData["Title"] = "Tài khoản hệ thống";

        var query = new GetPagedAccountsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = search,
            IsCustomerGroup = false
        };

        var roles = await roleManager.Roles
            .Where(r => r.Name != RoleNames.Customer)
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync();
        
        ViewBag.AvailableRoles = roles;

        var availablePermissions = typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Select(fi => (string)fi.GetValue(null)!)
            .OrderBy(p => p)
            .ToList();

        ViewBag.AvailablePermissions = availablePermissions;

        var result = await bus.InvokeAsync<PagedResult<AccountDto>>(query);
        return View(result);
    }

    /// <summary>
    /// Fetches account details for editing via AJAX.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.AccountsManage)]
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await bus.InvokeAsync<SystemAccountDetailDto?>(new GetAccountDetailsQuery(id));
        if (result == null) return NotFound();
        return Json(result);
    }

    /// <summary>
    /// Updates account roles and permissions via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.AccountsManage)]
    public async Task<IActionResult> Update([FromBody] UpdateSystemAccountCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Đã cập nhật tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new system account via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.AccountsManage)]
    public async Task<IActionResult> Create([FromBody] CreateSystemAccountCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Đã tạo tài khoản hệ thống thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Locks an account via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.AccountsLock)]
    public async Task<IActionResult> Lock([FromBody] LockUnlockRequest request)
    {
        try
        {
            await bus.InvokeAsync(new LockAccountCommand { AccountId = request.AccountId });
            return Json(new { success = true, message = "Đã khóa tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Unlocks an account via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.AccountsUnlock)]
    public async Task<IActionResult> Unlock([FromBody] LockUnlockRequest request)
    {
        try
        {
            await bus.InvokeAsync(new UnlockAccountCommand { AccountId = request.AccountId });
            return Json(new { success = true, message = "Đã mở khóa tài khoản thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Force-resets an account password via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.AccountsManage)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordAdminRequest request)
    {
        try
        {
            var newPassword = await bus.InvokeAsync<string>(new ResetAccountPasswordCommand(request.AccountId));
            return Json(new { success = true, message = "Mật khẩu đã được đặt lại thành công!", newPassword });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public record ResetPasswordAdminRequest(Guid AccountId);
    public record LockUnlockRequest(Guid AccountId);
}

