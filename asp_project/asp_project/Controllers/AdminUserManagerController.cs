using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using asp_project.Models;
using asp_project.Models.ViewModels; 
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using System.Threading.Tasks;
using System.Linq; 
using System;
using System.Collections.Generic;

namespace asp_project.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminHomeController : Controller
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Order> _orderCollection;

        public AdminHomeController(IMongoDatabase database)
        {
            // Chỉ cần kết nối 2 bảng này
            _userCollection = database.GetCollection<User>("User"); // Khớp với ảnh bạn gửi (số ít)
            _orderCollection = database.GetCollection<Order>("Orders"); // Số nhiều
        }

        public async Task<IActionResult> Index()
        {
            // --- BƯỚC 1: LẤY DỮ LIỆU THÔ (LẤY HẾT VỀ RAM XỬ LÝ CHO NHANH) ---
             
             
            // Lấy toàn bộ User (bất kể role gì)
            var allUsers = await _userCollection.Find(_ => true).ToListAsync();
            
            // Lấy toàn bộ Order
            var allOrders = await _orderCollection.Find(_ => true).ToListAsync();

        
            long totalUsers = allUsers.Count(u => u.Role?.ToLower() == "user");

            // Đếm Shop: Chỉ tính ai có Role là "shop"
            long totalShops = allUsers.Count(u => u.Role?.ToLower() == "shop");

            // Tính tiền
            decimal totalRevenue = allOrders.Sum(o => o.TotalAmount);
            decimal platformProfit = totalRevenue * 0.1m;
            int totalOrders = allOrders.Count;

            // --- BƯỚC 3: XỬ LÝ BIỂU ĐỒ (LOOP 6 THÁNG) ---
            
            var chartLabels = new List<string>();
            var userGrowth = new List<int>();
            var shopGrowth = new List<int>();
            var revenueGrowth = new List<decimal>();
            var profitGrowth = new List<decimal>();

            var now = DateTime.Now;

            for (int i = 5; i >= 0; i--) 
            {
                var targetDate = now.AddMonths(-i);
                var month = targetDate.Month;
                var year = targetDate.Year;

                chartLabels.Add($"Tháng {month}/{year}");

                // 1. Biểu đồ User (Dựa vào Role 'user' trong bảng User)
                var uCount = allUsers.Count(u => 
                    u.Role?.ToLower() == "user" && 
                    u.CreatedAt.Month == month && 
                    u.CreatedAt.Year == year);
                userGrowth.Add(uCount);

                // 2. Biểu đồ Shop (Dựa vào Role 'shop' trong bảng User)
                var sCount = allUsers.Count(u => 
                    u.Role?.ToLower() == "shop" && 
                    u.CreatedAt.Month == month && 
                    u.CreatedAt.Year == year);
                shopGrowth.Add(sCount);

                // 3. Biểu đồ Doanh thu
                var rSum = allOrders
                    .Where(o => o.CreatedAt.Month == month && o.CreatedAt.Year == year)
                    .Sum(o => o.TotalAmount);
                
                revenueGrowth.Add(rSum);
                profitGrowth.Add(rSum * 0.1m);
            }

            // --- BƯỚC 4: TRẢ VỀ VIEW ---
            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalShops = totalShops,
                TotalRevenue = totalRevenue,
                PlatformProfit = platformProfit,
                TotalOrders = totalOrders,

                ChartLabels = chartLabels,
                UserGrowthData = userGrowth,
                ShopGrowthData = shopGrowth,
                RevenueGrowthData = revenueGrowth,
                ProfitGrowthData = profitGrowth
            };

            return View(model);
        }
    }
}