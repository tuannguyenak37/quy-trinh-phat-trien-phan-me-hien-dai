// Trong /Controllers/OrderController.cs
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
    [Authorize] // BẮT BUỘC ĐĂNG NHẬP
    public class OrderController : Controller
    {
        private readonly IMongoCollection<Order> _ordersCollection;

        public OrderController(IMongoDatabase database)
        {
            _ordersCollection = database.GetCollection<Order>("Orders");
        }

        // Hàm helper để lấy ID của user đang đăng nhập
        private string GetCurrentUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        // -------------------------------------------------
        // 1. TRANG DANH SÁCH ĐƠN HÀNG (THÔNG TIN CƠ BẢN)
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Tìm tất cả đơn hàng thuộc về UserId này
            // Sắp xếp theo ngày tạo mới nhất
            var myOrders = await _ordersCollection
                .Find(o => o.UserId == userId)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(myOrders);
        }

        // -------------------------------------------------
        // 2. TRANG CHI TIẾT ĐƠN HÀNG
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var order = await _ordersCollection
                .Find(o => o.Id == id && o.UserId == userId)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            return View(order);
        }
        
        // -------------------------------------------------
        // 3. HÀNH ĐỘNG: XÁC NHẬN ĐÃ NHẬN HÀNG
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivery(string id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            
            var filter = Builders<Order>.Filter.Eq(o => o.Id, id) &
                         Builders<Order>.Filter.Eq(o => o.UserId, userId) &
                         Builders<Order>.Filter.Eq(o => o.Status, "Shipping"); 

            // Cập nhật trạng thái mới
            var update = Builders<Order>.Update.Set(o => o.Status, "Delivered"); 

            var result = await _ordersCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                // Không tìm thấy đơn, hoặc đơn không ở trạng thái "Shipping"
                TempData["ErrorMessage"] = "Không thể xác nhận đơn hàng này.";
            }
            else
            {
                TempData["SuccessMessage"] = "Đã xác nhận nhận hàng thành công!";
            }

            // Quay lại trang danh sách đơn hàng
            return RedirectToAction("Index");
        }
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CancelOrder(string id)
{
    var userId = GetCurrentUserId();
    if (string.IsNullOrEmpty(userId))
    {
        return Unauthorized();
    }

   
    var filter = Builders<Order>.Filter.Eq(o => o.Id, id) &
                 Builders<Order>.Filter.Eq(o => o.UserId, userId) &
                 Builders<Order>.Filter.Eq(o => o.Status, "Pending");

    // Cập nhật trạng thái sang "Canceled"
    var update = Builders<Order>.Update.Set(o => o.Status, "Canceled");

    var result = await _ordersCollection.UpdateOneAsync(filter, update);

    if (result.ModifiedCount > 0)
    {
        TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công!";
    }
    else
    {
        
        TempData["ErrorMessage"] = "Không thể hủy đơn hàng này (có thể đơn đã được xử lý).";
    }

    return RedirectToAction("Index");
}
    }
}