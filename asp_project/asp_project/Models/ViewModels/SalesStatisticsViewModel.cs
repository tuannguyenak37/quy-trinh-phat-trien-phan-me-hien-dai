// Trong /ViewModels/SalesStatisticsViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace asp_project.ViewModels
{
    public class SalesStatisticsViewModel
    {
        // Dùng để điền lại vào form filter
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        // --- Kết quả thống kê ---

        [DisplayFormat(DataFormatString = "{0:N0}")]
        public long TotalRevenue { get; set; } // Tổng doanh thu
        
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public long TotalOrders { get; set; } // Tổng số đơn hàng
    }
}