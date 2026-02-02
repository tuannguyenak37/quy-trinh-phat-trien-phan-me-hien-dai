using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using asp_project.Models;
using asp_project.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace asp_project.Controllers
{
    public class SearchController : Controller
    {
        private readonly IMongoCollection<Product> _productsCollection;

        // SỬA LỖI 1: Dùng IMongoDatabase thay vì IMongoClient
        // Lý do: Trong Startup.cs bạn chỉ đăng ký IMongoDatabase, nên dùng cái này mới chạy được.
        public SearchController(IMongoDatabase database)
        {
            _productsCollection = database.GetCollection<Product>("Products");
        }

        // --- 1. TRANG KẾT QUẢ TÌM KIẾM (Full Page) ---
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string q)
        {
            var viewModel = new SearchViewModel
            {
                SearchQuery = q,
                Results = new List<Product>()
            };

            if (string.IsNullOrWhiteSpace(q))
            {
                return View(viewModel);
            }

            // Tạo bộ lọc Full-Text Search
            var textFilter = Builders<Product>.Filter.Text(q, new TextSearchOptions
            {
                CaseSensitive = false,
                DiacriticSensitive = false
            });

            // Lọc sản phẩm hiển thị
            var visibleFilter = Builders<Product>.Filter.Eq(p => p.IsVisible, true);
            var finalFilter = Builders<Product>.Filter.And(textFilter, visibleFilter);

            // Sắp xếp theo độ khớp (Score)
            var sort = Builders<Product>.Sort.MetaTextScore("textScore");

            // Chỉ lấy các trường cần thiết (Projection)
            // Lưu ý: Ở đây chỉ INCLUDE (lấy về), không biến đổi dữ liệu nên không bị lỗi.
            var projection = Builders<Product>.Projection
                                            .Include(p => p.Id)
                                            .Include(p => p.Ten)
                                            .Include(p => p.HinhAnh)
                                            .Include(p => p.Skus); // Cần Skus để hiển thị giá

            viewModel.Results = await _productsCollection
                .Find(finalFilter)
                .Sort(sort)
                .Project<Product>(projection) // Map kết quả về object Product
                .Limit(24)
                .ToListAsync();

            return View(viewModel);
        }

        // --- 2. API LẤY GỢI Ý (ĐÃ SỬA LỖI LOGIC) ---
        [HttpGet]
        public async Task<IActionResult> GetSuggestions(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(new List<object>());
            }

            // Tạo filter
            var textFilter = Builders<Product>.Filter.Text(q, new TextSearchOptions
            {
                CaseSensitive = false,
                DiacriticSensitive = false
            });
            var visibleFilter = Builders<Product>.Filter.Eq(p => p.IsVisible, true);
            var finalFilter = Builders<Product>.Filter.And(textFilter, visibleFilter);
            var sort = Builders<Product>.Sort.MetaTextScore("textScore");

            // SỬA LỖI 2: Tách quá trình lấy dữ liệu và xử lý ảnh
            
            // BƯỚC A: Chỉ định các trường cần lấy từ MongoDB (Projection)
            var projection = Builders<Product>.Projection
                .Include(p => p.Id)
                .Include(p => p.Ten)
                .Include(p => p.HinhAnh);

            // BƯỚC B: Tải dữ liệu thô về RAM (Dùng ToListAsync)
            // Chúng ta KHÔNG dùng .Select() hay .Project(new {}) ở đây để tránh lỗi Driver
            var rawProducts = await _productsCollection
                .Find(finalFilter)
                .Project<Product>(projection) 
                .Sort(sort)
                .Limit(5)
                .ToListAsync(); 

            // BƯỚC C: Xử lý dữ liệu trên RAM bằng C# thuần
            // Lúc này dùng FirstOrDefault() thoải mái
            var suggestions = rawProducts.Select(p => new
            {
                Id = p.Id,
                Ten = p.Ten,
                // Logic an toàn: Nếu có ảnh thì lấy cái đầu, không thì để chuỗi rỗng
                Image = (p.HinhAnh != null && p.HinhAnh.Count > 0) ? p.HinhAnh[0] : ""
            });

            return Ok(suggestions);
        }
    }
}