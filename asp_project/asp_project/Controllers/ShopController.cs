using asp_project.Dtos;
using asp_project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using MongoDB.Driver; // <--- DÙNG THƯ VIỆN GỐC
using MongoDB.Bson;

namespace asp_project.Controllers
{
    [Authorize]
    public class ShopController : Controller
    {
        // Thay AppDbContext bằng IMongoCollection
        private readonly IMongoCollection<Shop> _shopCollection;
        private readonly IMongoCollection<User> _userCollection;
        private readonly IWebHostEnvironment _env;

        public ShopController(IMongoDatabase database, IWebHostEnvironment env)
        {
            _env = env;
            // Kết nối đúng tên bảng "Shop" và "User" (số ít) như bạn đã cấu hình
            _shopCollection = database.GetCollection<Shop>("Shop");
            _userCollection = database.GetCollection<User>("User");
        }

        // GET: /Shop/RegisterShopView
        public IActionResult RegisterShopView()
        {
            return View(); 
        }

        // POST: /Shop/RegisterShop
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterShop(ShopRegistrationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không tìm thấy thông tin người dùng.");

            // 1. Kiểm tra user đã có shop chưa (Dùng Driver)
            var existingShop = await _shopCollection.Find(s => s.UserId == userId).FirstOrDefaultAsync();
            if (existingShop != null)
            {
                ModelState.AddModelError("", "Bạn đã đăng ký shop rồi.");
                return View("RegisterShopView", dto);
            }

            // 2. Xử lý avatar upload
            string? avatarUrl = null;
            if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.AvatarFile.FileName);
                var filePath = Path.Combine(uploadDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.AvatarFile.CopyToAsync(stream);
                }
                avatarUrl = "/uploads/" + fileName;
            }

            // 3. Tạo đối tượng Shop mới
            var newShop = new Shop
            {
                // [QUAN TRỌNG] Tự sinh ID để tránh lỗi null
                Id = ObjectId.GenerateNewId().ToString(),

                ShopName = dto.ShopName,
                Phone = dto.Phone,
                CCCD = dto.CCCD,
                Description = dto.Description,
                Address = dto.Address,
                CategoryIds = dto.CategoryIds ?? new List<string>(),
                AvatarUrl = avatarUrl,
                UserId = userId,
                IsActive = true, 
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // 4. Lưu shop (Dùng InsertOneAsync)
                await _shopCollection.InsertOneAsync(newShop);

                // 5. Cập nhật Role User thành "shop" (Dùng UpdateOneAsync)
                var userFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
                var userUpdate = Builders<User>.Update.Set(u => u.Role, "shop");
                
                await _userCollection.UpdateOneAsync(userFilter, userUpdate);

                // 6. Lấy thông tin User để cập nhật Cookie
                var user = await _userCollection.Find(userFilter).FirstOrDefaultAsync();

                // Refresh cookie để có quyền Shop ngay lập tức
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Role, "shop"), // Role mới
                    new Claim("ShopId", newShop.Id)     // ShopId mới
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // Chuyển hướng về trang Login (theo code cũ của bạn) hoặc trang quản lý Shop
                return RedirectToAction("Login", "Account"); 
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View("RegisterShopView", dto);
            }
        }
    }
}