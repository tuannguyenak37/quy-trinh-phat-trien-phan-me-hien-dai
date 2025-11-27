// Trong /Controllers/ShopOrderController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using asp_project.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace asp_project.Controllers
{
    [Authorize(Roles = "shop")] 
    public class ShopOrderController : Controller
    {
        private readonly IMongoCollection<Order> _ordersCollection;
        private readonly IMongoCollection<Product> _productsCollection;

        public ShopOrderController(IMongoDatabase database)
        {
            _ordersCollection = database.GetCollection<Order>("Orders");
            _productsCollection = database.GetCollection<Product>("Products");
        }

        // Hàm helper để lấy ShopId của shop đang đăng nhập
        private string GetCurrentShopId()
        {
            // (Giả sử bạn lưu ShopId trong Claim "ShopId" khi đăng nhập)
            return User.Claims.FirstOrDefault(c => c.Type == "ShopId")?.Value;
        }

        // -------------------------------------------------
        // 1. TRANG DANH SÁCH ĐƠN HÀNG CỦA SHOP
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized("Không tìm thấy ShopId.");

            // (QUAN TRỌNG) Tìm tất cả đơn hàng
            // mà mảng 'ShopIds' CÓ CHỨA ShopId của shop này
            var filter = Builders<Order>.Filter.AnyEq(o => o.ShopIds, shopId);
            
            var shopOrders = await _ordersCollection
                .Find(filter)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(shopOrders);
        }

        // -------------------------------------------------
        // 2. TRANG CHI TIẾT ĐƠN HÀNG (DÀNH CHO SHOP)
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized();

            // (Bảo mật) Tìm đơn hàng phải khớp OrderId VÀ ShopId
            var filter = Builders<Order>.Filter.Eq(o => o.Id, id) &
                         Builders<Order>.Filter.AnyEq(o => o.ShopIds, shopId);
            
            var order = await _ordersCollection.Find(filter).FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn này.");
            }

            // (Logic) Chỉ hiển thị các sản phẩm thuộc shop này
            // Lọc lại danh sách Items
            var shopProductIds = (await _productsCollection.Find(p => p.ShopId == shopId).Project(p => p.Id).ToListAsync()).ToHashSet();
            
            // Chỉ giữ lại các items mà ProductId thuộc về shop
            order.Items = order.Items.Where(item => shopProductIds.Contains(item.ProductId)).ToList();

            return View(order);
        }

        // -------------------------------------------------
        // 3. HÀNH ĐỘNG: CẬP NHẬT TRẠNG THÁI
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string newStatus)
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized();

            
            var filter = Builders<Order>.Filter.Eq(o => o.Id, id) &
                         Builders<Order>.Filter.AnyEq(o => o.ShopIds, shopId);

            if (newStatus != "Processing" && newStatus != "Shipping" && newStatus != "Cancelled")
            {
                TempData["ErrorMessage"] = "Trạng thái cập nhật không hợp lệ.";
                return RedirectToAction("Index");
            }
            
            var update = Builders<Order>.Update.Set(o => o.Status, newStatus);

            var result = await _ordersCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật đơn hàng.";
            }

            return RedirectToAction("Index");
        }
    }
}