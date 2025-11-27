using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using asp_project.Models;
using asp_project.ViewModels;
using MongoDB.Driver; // <--- DÙNG THƯ VIỆN GỐC ĐỂ TRÁNH LỖI
using BCrypt.Net;
using MongoDB.Bson;
using System;

namespace asp_project.Controllers
{
    public class AccountController : Controller
    {
        // Thay vì AppDbContext, ta dùng IMongoCollection trực tiếp
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Shop> _shopCollection;

        public AccountController(IMongoDatabase database)
        {
            // KẾT NỐI ĐÚNG TÊN COLLECTION (User, Shop số ít như ảnh bạn gửi)
            _userCollection = database.GetCollection<User>("User"); 
            _shopCollection = database.GetCollection<Shop>("Shop");
        }

        // ==========================================
        // 1. ĐĂNG KÝ (REGISTER)
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra Email tồn tại (Dùng Find của Driver)
                var existingUser = await _userCollection.Find(u => u.Email == model.Email).FirstOrDefaultAsync();
                
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(model);
                }
                // 2. Hash mật khẩu
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                var newUser = new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Birthday = model.Birthday,
                    Email = model.Email,
                    Password = hashedPassword,
                    Role = "user", 
                    CreatedAt = DateTime.Now
                };
                await _userCollection.InsertOneAsync(newUser);

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // ==========================================
        // 2. ĐĂNG NHẬP (LOGIN)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Tìm User
                var user = await _userCollection.Find(u => u.Email == model.Email).FirstOrDefaultAsync();

                // 2. Kiểm tra mật khẩu
                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    // --- TẠO CLAIMS ---
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, $"{user.LastName} {user.FirstName}"),
                        new Claim(ClaimTypes.Role, user.Role) 
                    };

                    // 3. Tìm Shop của user (Dùng Find của Driver)
                    var shop = await _shopCollection.Find(s => s.UserId == user.Id).FirstOrDefaultAsync();

                    if (shop != null)
                    {
                        // [KIỂM TRA CẤM SHOP]
                        if (shop.IsActive == false)
                        {
                            ModelState.AddModelError(string.Empty, "Tài khoản Shop của bạn đã bị KHÓA do vi phạm chính sách. Vui lòng liên hệ Admin qua email 23050150@student.bdu.vn.");
                            return View(model); // Chặn đăng nhập
                        }

                        // Cấp quyền Shop
                        claims.Add(new Claim(ClaimTypes.Role, "shop"));
                        claims.Add(new Claim("ShopId", shop.Id));
                    }

                    // 4. Ghi Cookie
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 5. ĐIỀU HƯỚNG
                    
                    // Admin -> Dashboard
                    if (user.Role == "admin")
                    {
                        return RedirectToAction("Index", "AdminHome");
                    }

                    // Shop -> Trang quản lý Shop
                    if (user.Role == "shop" || shop != null)
                    {
                        
                         return RedirectToAction("Index", "MyStore");
                    }

                    // User -> Trang chủ
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        // ==========================================
        // 3. ĐĂNG XUẤT
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}