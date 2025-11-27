using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using asp_project.Models; // Đảm bảo bạn using model Product

namespace asp_project.Controllers
{
    // KHÔNG CÓ [Authorize] ở đây
    public class ProductPageController : Controller
    {
        private readonly IMongoCollection<Product> _productsCollection;

        //  inject IMongoDatabase 
        public ProductPageController(IMongoDatabase database)
        {
            _productsCollection = database.GetCollection<Product>("Products");
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

            // *** ĐIỀU QUAN TRỌNG NHẤT ***
            // Tìm sản phẩm theo ID VÀ sản phẩm đó PHẢI ĐANG "HIỂN THỊ" (IsVisible == true)
            var product = await _productsCollection
                .Find(p => p.Id == id && p.IsVisible == true)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                // Không tìm thấy,
                // hoặc sản phẩm đang bị "Ẩn" (IsVisible == false)
                return NotFound("Không tìm thấy sản phẩm.");
            }

            // Trả về View với dữ liệu sản phẩm
            return View(product);
        }

        // Bạn có thể thêm các trang public khác ở đây, 
        // ví dụ: trang chủ, trang danh sách sản phẩm...
    }
}