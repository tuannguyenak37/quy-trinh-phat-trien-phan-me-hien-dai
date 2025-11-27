using System.Collections.Generic;

namespace asp_project.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // --- SỐ LIỆU TỔNG QUAN (CŨ) ---
        public long TotalUsers { get; set; }
        public long TotalShops { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PlatformProfit { get; set; }
        public int TotalOrders { get; set; }

        // --- DỮ LIỆU CHO BIỂU ĐỒ (MỚI) ---
        
        // 1. Danh sách các tháng (Trục ngang: "Tháng 1", "Tháng 2"...)
        public List<string> ChartLabels { get; set; } = new List<string>();

        // 2. Dữ liệu tăng trưởng User theo tháng
        public List<int> UserGrowthData { get; set; } = new List<int>();

        // 3. Dữ liệu tăng trưởng Shop theo tháng
        public List<int> ShopGrowthData { get; set; } = new List<int>();

        // 4. Dữ liệu doanh thu theo tháng
        public List<decimal> RevenueGrowthData { get; set; } = new List<decimal>();
        
        // 5. Dữ liệu lợi nhuận theo tháng
        public List<decimal> ProfitGrowthData { get; set; } = new List<decimal>();
        public long ActiveShops { get; set; }
public long BannedShops { get; set; }
    }
}