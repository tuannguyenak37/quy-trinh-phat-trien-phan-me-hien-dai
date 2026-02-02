using System.Collections.Generic;

namespace asp_project.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        // Danh sách 1: Top sản phẩm bán chạy
        public List<TopProductViewModel> TopSellingProducts { get; set; } = new List<TopProductViewModel>();

        // Danh sách 2: Sản phẩm gợi ý
        public List<TopProductViewModel> RecommendedProducts { get; set; } = new List<TopProductViewModel>();
    }
}