using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using asp_project.Models; 
using asp_project.Models.ViewModels;
using System.Collections.Generic;

namespace asp_project.Controllers
{
    // KHÔNG CÓ [Authorize] ở đây
    public class ProductPageController : Controller
    {
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly IMongoCollection<Shop> _shopCollection;

        //  inject IMongoDatabase 
        public ProductPageController(IMongoDatabase database)
        {
            _productsCollection = database.GetCollection<Product>("Products");
            _shopCollection = database.GetCollection<Shop>("Shop");
        }

        // -------------------------------------
        // HIỂN THỊ CHI TIẾT SẢN PHẨM (PUBLIC)
        // -------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Không có ID sản phẩm.");
            }

            // 1. Lấy sản phẩm
            var product = await _productsCollection
                .Find(p => p.Id == id && p.IsVisible == true)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            // 2. Lấy thông tin Shop
            Shop shop = null;
            if (!string.IsNullOrEmpty(product.ShopId))
            {
                shop = await _shopCollection.Find(s => s.Id == product.ShopId).FirstOrDefaultAsync();
            }

            // 3. Lấy sản phẩm liên quan (Cùng danh mục, khác ID này)
            var relatedProducts = new List<Product>();
            if (product.DanhMuc != null && product.DanhMuc.Count > 0)
            {
                var categoryToFind = product.DanhMuc[0];
                relatedProducts = await _productsCollection
                    .Find(p => p.DanhMuc.Contains(categoryToFind) && p.Id != id && p.IsVisible == true)
                    .Limit(4)
                    .ToListAsync();
            }

            // 4. Đóng gói vào ViewModel
            var viewModel = new ProductPageDetailViewModel
            {
                Product = product,
                Shop = shop,
                RelatedProducts = relatedProducts
            };

            // Trả về View với ViewModel mới
            return View(viewModel);
        }
    }
}