// Trong /ViewModels/ShopInventoryViewModel.cs
using System.Collections.Generic;

namespace asp_project.ViewModels
{
    public class ShopInventoryViewModel
    {
        public List<ProductStockUpdateModel> Products { get; set; } = new List<ProductStockUpdateModel>();
    }

    public class ProductStockUpdateModel
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public List<SkuStockUpdateModel> Skus { get; set; } = new List<SkuStockUpdateModel>();
    }

    public class SkuStockUpdateModel
    {
        public string SkuCode { get; set; }
        public string SkuName { get; set; }
        
        public string ImageUrl { get; set; } = string.Empty; // <-- (MỚI) Thêm trường ảnh

        public int TonKho { get; set; }    
        public int SoLuongThem { get; set; } = 0; 
    }
}