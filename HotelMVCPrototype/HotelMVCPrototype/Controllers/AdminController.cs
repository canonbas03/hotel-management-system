using HotelMVCPrototype.Data;
using HotelMVCPrototype.Models.Enums;
using HotelMVCPrototype.Models.ViewModels;
using HotelMVCPrototype.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var model = new List<UserWithRolesViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserWithRolesViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Roles = roles.ToList()
            });
        }

        return View(model);
    }

    public async Task<IActionResult> EditRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var allRoles = _roleManager.Roles.ToList();

        var model = new EditRolesViewModel
        {
            UserId = user.Id,
            Email = user.Email,
            Roles = allRoles.Select(r => new RoleCheckbox
            {
                RoleName = r.Name,
                IsSelected = _userManager.IsInRoleAsync(user, r.Name).Result
            }).ToList()
        };

        return View(model);
    }

    // POST: Save changes
    [HttpPost]
    public async Task<IActionResult> EditRoles(EditRolesViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName);

        await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        return RedirectToAction("Index");
    }

    public IActionResult CreateStaff()
    {
        var model = new CreateStaffUserViewModel
        {
            Roles = _roleManager.Roles.Select(r => r.Name).ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStaff(CreateStaffUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            model.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.SelectedRole);

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetActiveRole(string role, string? returnUrl = null)
    {
        var allowed = new[] { "Reception", "Housekeeping", "Bar", "Maintenance", "Security", "Admin" };
        if (!allowed.Contains(role)) return BadRequest();

        HttpContext.Session.SetString("ActiveRole", role);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}

