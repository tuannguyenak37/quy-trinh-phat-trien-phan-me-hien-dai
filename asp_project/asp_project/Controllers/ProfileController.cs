using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using asp_project.Models;
using asp_project.Models.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver; // ✅ Dùng thư viện gốc như AccountController
using BCrypt.Net;

namespace asp_project.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
       
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Shop> _shopCollection;

        public ProfileController(IMongoDatabase database)
        {
            // Kết nối đúng tên Collection như trong AccountController
            _userCollection = database.GetCollection<User>("User");
            _shopCollection = database.GetCollection<Shop>("Shop");
        }

        // GET: /Profile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // ✅ Dùng hàm Find của Driver (Async)
            var user = await _userCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            
            if (user == null) return NotFound();

            var infoModel = new UserProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Birthday = user.Birthday.ToLocalTime(),
                Role = user.Role
            };

            return View(infoModel);
        }

        // POST: /Profile/UpdateInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInfo(UserProfileViewModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != model.Id) return Forbid();

            if (ModelState.IsValid)
            {
                var userInDb = await _userCollection.Find(u => u.Id == model.Id).FirstOrDefaultAsync();
                if (userInDb == null) return NotFound();

                // Cập nhật thông tin trên object
                userInDb.FirstName = model.FirstName;
                userInDb.LastName = model.LastName;
                userInDb.Birthday = model.Birthday;
                var filter = Builders<User>.Filter.Eq(u => u.Id, userInDb.Id);
                await _userCollection.ReplaceOneAsync(filter, userInDb);

                await RefreshSignInAsync(userInDb);

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                TempData["ActiveTab"] = "info";
                return RedirectToAction("Index");
            }

            TempData["ActiveTab"] = "info";
            return View("Index", model);
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

                if (user == null) return NotFound();

                // 1. Kiểm tra mật khẩu cũ
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                    TempData["ActiveTab"] = "password";
                    return ReturnIndexWithErrors(user);
                }

                // 2. Hash mật khẩu mới
                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

                var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
                await _userCollection.ReplaceOneAsync(filter, user);

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                TempData["ActiveTab"] = "password";
                return RedirectToAction("Index");
            }

            TempData["ActiveTab"] = "password";
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
            return ReturnIndexWithErrors(currentUser);
        }

        // Helper để tái tạo view khi lỗi (Giữ code gọn gàng)
        private IActionResult ReturnIndexWithErrors(User user)
        {
            if (user == null) return NotFound();
            var infoModel = new UserProfileViewModel
            {
                Id = user.Id, Email = user.Email, FirstName = user.FirstName,
                LastName = user.LastName, Birthday = user.Birthday, Role = user.Role
            };
            ViewBag.PasswordErrors = ModelState;
            return View("Index", infoModel);
        }

        private async Task RefreshSignInAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // ✅ Lấy Shop dùng _shopCollection (Driver)
            var shop = await _shopCollection.Find(s => s.UserId == user.Id).FirstOrDefaultAsync();
            if (shop != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, "shop"));
                claims.Add(new Claim("ShopId", shop.Id));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }
    }
}