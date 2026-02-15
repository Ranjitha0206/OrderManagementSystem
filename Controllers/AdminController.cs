using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace OrderManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();

            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new
                {
                    user.Id,
                    user.Email,
                    Role = roles.FirstOrDefault()
                });

            }
            return View(userList);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PromoteToManager(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(Users));

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction(nameof(Users));

            // Prevent changing Admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction(nameof(Users));

            // Only promote if currently User
            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _userManager.AddToRoleAsync(user, "Manager");
            }

            return RedirectToAction(nameof(Users));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DemoteToUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction(nameof(Users));

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction(nameof(Users));

            // Never modify Admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction(nameof(Users));

            // Only demote if currently Manager
            if (await _userManager.IsInRoleAsync(user, "Manager"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Manager");
                await _userManager.AddToRoleAsync(user, "User");
            }

            return RedirectToAction(nameof(Users));
        }



    }
}
