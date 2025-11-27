// /Controllers/SearchController.cs
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using asp_project.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using asp_project.ViewModels; 

namespace asp_project.Controllers
{
    public class SearchController : Controller
    {
        private readonly IMongoCollection<Product> _productsCollection;

        public SearchController(IMongoDatabase database)
        {
            _productsCollection = database.GetCollection<Product>("Products");
        }

        // --- TRANG KẾT QUẢ LỚN (ĐÃ SỬA LỖI) ---
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

            var filter = Builders<Product>.Filter.Text(q, new TextSearchOptions 
            { 
                CaseSensitive = false, 
                DiacriticSensitive = false 
            });
            
            var visibleFilter = Builders<Product>.Filter.Eq(p => p.IsVisible, true);
            var finalFilter = Builders<Product>.Filter.And(filter, visibleFilter);

            // 1. Sort vẫn giữ nguyên (dùng textScore trên server)
            var sort = Builders<Product>.Sort.MetaTextScore("textScore");
            
            // --- (PHẦN ĐÃ SỬA) ---
            // 2. Chỉ Project (chiếu) các trường CÓ TỒN TẠI trong Model
            //    (Xóa .MetaTextScore("textScore") khỏi đây)
            var projection = Builders<Product>.Projection
                                .Include(p => p.Id)
                                .Include(p => p.Ten)
                                .Include(p => p.HinhAnh)
                                .Include(p => p.Skus); // Cần để lấy giá
            // --- (HẾT PHẦN SỬA) ---

            // 3. Thực thi tìm kiếm
            viewModel.Results = await _productsCollection
                .Find(finalFilter) 
                .Sort(sort)        
                .Project<Product>(projection) // Bây giờ sẽ hoạt động
                .Limit(24) 
                .ToListAsync();
                
            return View(viewModel);
        }

        // --- API LẤY GỢI Ý (Giữ nguyên, code này đã đúng) ---
        [HttpGet]
        public async Task<IActionResult> GetSuggestions(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(new List<object>()); 
            }

            var filter = Builders<Product>.Filter.Text(q, new TextSearchOptions 
            { 
                CaseSensitive = false, 
                DiacriticSensitive = false 
            });
            var visibleFilter = Builders<Product>.Filter.Eq(p => p.IsVisible, true);
            var finalFilter = Builders<Product>.Filter.And(filter, visibleFilter);

            var sort = Builders<Product>.Sort.MetaTextScore("textScore");
            
            var suggestions = await _productsCollection
                .Find(finalFilter)
                .Sort(sort)
                .Project(p => new { // Project vào kiểu vô danh (an toàn)
                    Id = p.Id,
                    Ten = p.Ten,
                    Image = p.HinhAnh.FirstOrDefault() ?? "" 
                })
                .Limit(5) 
                .ToListAsync();

            return Ok(suggestions);
        }
    }
}