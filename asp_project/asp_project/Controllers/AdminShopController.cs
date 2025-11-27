using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using asp_project.Models;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace asp_project.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminShopController : Controller
    {
        private readonly IMongoCollection<Shop> _shopCollection;

        public AdminShopController(IMongoDatabase database)
        {
            // Kết nối bảng "Shop" (số ít)
            _shopCollection = database.GetCollection<Shop>("Shop");
        }

        // ==========================================
        // 1. DANH SÁCH & TÌM KIẾM & THỐNG KÊ
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string searchString)
        {
            var builder = Builders<Shop>.Filter;
            var filter = builder.Empty;

            
            if (!string.IsNullOrEmpty(searchString))
            {
                filter = builder.Regex(s => s.ShopName, new BsonRegularExpression(searchString, "i"));
            }

            // --- B. LẤY DANH SÁCH HIỂN THỊ ---
            // Sắp xếp: Mới nhất lên đầu
            var sort = Builders<Shop>.Sort.Descending(s => s.CreatedAt);
            var shops = await _shopCollection.Find(filter).Sort(sort).ToListAsync();

            // --- C. THỐNG KÊ (ĐỂ HIỂN THỊ TRÊN ĐẦU TRANG) ---
            // Dùng CountDocumentsAsync để đếm nhanh trong DB mà không tốn RAM tải list về
            long activeShops = await _shopCollection.CountDocumentsAsync(s => s.IsActive == true);
            long bannedShops = await _shopCollection.CountDocumentsAsync(s => s.IsActive == false);

            ViewBag.CurrentFilter = searchString;
            ViewBag.ActiveCount = activeShops; // Số shop đang hoạt động
            ViewBag.BannedCount = bannedShops; // Số shop bị cấm

            return View(shops);
        }

        // ==========================================
        // 2. CẤM / MỞ KHÓA SHOP
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var shop = await _shopCollection.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (shop == null) return NotFound();

            bool newStatus = !shop.IsActive;

            var update = Builders<Shop>.Update.Set(s => s.IsActive, newStatus);
            await _shopCollection.UpdateOneAsync(s => s.Id == id, update);

            // Thông báo ra màn hình
            string statusMsg = newStatus ? "đã được KÍCH HOẠT lại" : "đã bị CẤM hoạt động";
            TempData["Message"] = $"Shop '{shop.ShopName}' {statusMsg}.";
            
            return RedirectToAction(nameof(Index));
        }
    }
}