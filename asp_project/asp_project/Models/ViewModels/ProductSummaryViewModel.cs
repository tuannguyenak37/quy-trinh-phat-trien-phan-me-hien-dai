namespace asp_project.Models.ViewModels
{
    public class ProductSummaryViewModel
    {
        public string Id { get; set; } = null!;
        public string Ten { get; set; } = string.Empty;

        /// <summary>
        /// URL của ảnh đầu tiên (hoặc null nếu không có ảnh).
        /// </summary>
        public string? FirstImage { get; set; }

        /// <summary>
        /// Giá đại diện (ví dụ: giá của SKU đầu tiên).
        /// </summary>
        public decimal Price { get; set; }
    }
}
