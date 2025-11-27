namespace asp_project.Models.ViewModels
{
    public class TopProductViewModel
    {
        public string ProductId { get; set; } = string.Empty;
        public string TenSanPham { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty; // Ảnh đại diện
        public decimal Gia { get; set; }    // Giá
        public int DaBan { get; set; }      // Tổng số lượng đã bán
    }
}