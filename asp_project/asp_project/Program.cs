using MongoFramework;
using asp_project.Models; // Nơi chứa Model
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver; // Cần cho IMongoDatabase
using Microsoft.AspNetCore.Hosting; // Cần cho IWebHostEnvironment

// (MỚI) Thêm 2 using này để sửa lỗi
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;


var builder = WebApplication.CreateBuilder(args);

// --- Cấu hình MongoFramework (Dùng cho Account, Shop) ---
// 1. Lấy connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Đăng ký kết nối (Scoped)
builder.Services.AddScoped<IMongoDbConnection>(s =>
    MongoDbConnection.FromConnectionString(connectionString)
);

// 3. Đăng ký DbContext (Scoped)
builder.Services.AddScoped<AppDbContext>();


// --- Cấu hình MongoDB Driver (Dùng cho Product) ---
// (Bạn CẦN phần này vì ProductsController đang dùng nó)
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings");
var client = new MongoClient(mongoSettings.GetValue<string>("ConnectionString"));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    return client.GetDatabase(mongoSettings.GetValue<string>("DatabaseName"));
});

// -----------------------------------------------------------------
// (MỚI) SỬA LỖI PASCALCASE/CAMELCASE TẠI ĐÂY
// -----------------------------------------------------------------
// Đăng ký quy ước chung: Tự động chuyển PascalCase (C#) sang camelCase (DB)
var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
ConventionRegistry.Register("CamelCase", conventionPack, t => true);
// -----------------------------------------------------------------


// 4. Add MVC (Giữ nguyên)
builder.Services.AddControllersWithViews();

// 5. Cấu hình Cookie Authentication (Giữ nguyên)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });


var app = builder.Build();

// 6. Cấu hình middleware (Giữ nguyên)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cần để load ảnh từ wwwroot

app.UseRouting();

// 7. Bật Authentication (Rất quan trọng)
app.UseAuthentication();

app.UseAuthorization(); // Giữ nguyên

// 8. Map route mặc định (Giữ nguyên)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();