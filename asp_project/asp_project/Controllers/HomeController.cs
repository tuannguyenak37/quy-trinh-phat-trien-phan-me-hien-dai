using System.Diagnostics;
using System.Security.Claims; // <--- QUAN TRỌNG: Sửa lỗi ClaimTypes và FindFirstValue
using asp_project.Models;
using asp_project.Models.ViewModels; 
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver; 

namespace asp_project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMongoCollection<Product> _productCollection;
        private readonly IMongoCollection<BsonDocument> _rawOrderCollection;
        private readonly IMongoCollection<Order> _orderCollection; 

        public HomeController(ILogger<HomeController> logger, IMongoDatabase database)
        {
            _logger = logger;
            _productCollection = database.GetCollection<Product>("Products");
            _rawOrderCollection = database.GetCollection<BsonDocument>("Orders");
            _orderCollection = database.GetCollection<Order>("Orders");
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel();

            try
            {
                // 1. Lấy Top Bán Chạy
                model.TopSellingProducts = await GetTopSellingProducts();

                // 2. Lấy Gợi Ý (Mới/Tương tự/Ngẫu nhiên)
                model.RecommendedProducts = await GetRecommendedProducts();

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine("LỖI HOME: " + ex.Message);
                // Trả về view trống để không sập web
                return View(new HomeIndexViewModel());
            }
        }

        // --- HÀM 1: TOP BÁN CHẠY (Logic đã fix chữ thường) ---
        private async Task<List<TopProductViewModel>> GetTopSellingProducts()
        {
            var pipeline = new List<BsonDocument>
            {
                // Dùng tên trường chữ thường khớp DB của bạn
                new BsonDocument("$match", new BsonDocument("status", new BsonDocument("$ne", "Cancelled"))),
                new BsonDocument("$unwind", "$items"), 
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$items.productId" }, 
                    { "TotalSold", new BsonDocument("$sum", "$items.quantity") }
                }),
                new BsonDocument("$sort", new BsonDocument("TotalSold", -1)),
                new BsonDocument("$limit", 5)
            };

            var topSellingRaw = await _rawOrderCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            if (topSellingRaw.Count == 0) return new List<TopProductViewModel>();

            // Lấy danh sách ID (Hỗ trợ cả String và ObjectId)
            var listStringIds = new List<string>();
            var listObjectIds = new List<ObjectId>();

            foreach (var item in topSellingRaw)
            {
                if (!item["_id"].IsBsonNull)
                {
                    string sId = item["_id"].ToString();
                    listStringIds.Add(sId);
                    if (ObjectId.TryParse(sId, out ObjectId oid)) listObjectIds.Add(oid);
                }
            }

            var filter = new BsonDocument("$or", new BsonArray {
                new BsonDocument("_id", new BsonDocument("$in", new BsonArray(listObjectIds))),
                new BsonDocument("_id", new BsonDocument("$in", new BsonArray(listStringIds)))
            });

            var products = await _productCollection.Find(filter).ToListAsync();
            var result = new List<TopProductViewModel>();

            foreach (var raw in topSellingRaw)
            {
                string rawId = raw["_id"].ToString();
                var prod = products.FirstOrDefault(p => p.Id == rawId || p.Id.ToString() == rawId);

                if (prod != null)
                {
                    decimal gia = (prod.Skus?.Any() == true) ? prod.Skus[0].GiaBan : 0;
                    string anh = "/images/no-image.png";
                    if (prod.HinhAnh?.Any() == true) anh = prod.HinhAnh[0];
                    else if (prod.Skus?.Any() == true && prod.Skus[0].HinhAnh?.Any() == true) anh = prod.Skus[0].HinhAnh[0];

                    result.Add(new TopProductViewModel 
                    { 
                        ProductId = prod.Id, 
                        TenSanPham = prod.Ten, 
                        Gia = gia, 
                        HinhAnh = anh, 
                        DaBan = raw["TotalSold"].IsInt32 ? raw["TotalSold"].AsInt32 : (int)raw["TotalSold"].AsDouble
                    });
                }
            }
            return result;
        }

        // --- HÀM 2: GỢI Ý SẢN PHẨM (Đã fix lỗi Sample & ClaimTypes) ---
        private async Task<List<TopProductViewModel>> GetRecommendedProducts()
        {
            // SỬA LỖI 1: Đã có using System.Security.Claims ở trên cùng
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            List<Product> products = new List<Product>();

            // A. NẾU ĐÃ ĐĂNG NHẬP -> TÌM THEO LỊCH SỬ
            if (!string.IsNullOrEmpty(userId))
            {
                var recentOrders = await _orderCollection.Find(o => o.UserId == userId)
                                                         .SortByDescending(o => o.CreatedAt)
                                                         .Limit(5)
                                                         .ToListAsync();

                if (recentOrders.Any())
                {
                    var boughtProductIds = recentOrders
                        .SelectMany(o => o.Items)
                        .Select(i => i.ProductId)
                        .Distinct()
                        .ToList();

                    if (boughtProductIds.Any())
                    {
                        var filterBought = Builders<Product>.Filter.In(p => p.Id, boughtProductIds);
                        var boughtProductsInfo = await _productCollection.Find(filterBought).ToListAsync();
                        var interestedCategories = boughtProductsInfo.SelectMany(p => p.DanhMuc).Distinct().ToList();

                        if (interestedCategories.Any())
                        {
                            // Tìm sản phẩm cùng danh mục trừ cái đã mua
                            var recommendationFilter = Builders<Product>.Filter.AnyIn(p => p.DanhMuc, interestedCategories) 
                                                     & !Builders<Product>.Filter.In(p => p.Id, boughtProductIds);

                            products = await _productCollection.Find(recommendationFilter)
                                                               .Limit(10)
                                                               .ToListAsync();
                        }
                    }
                }
            }

            // B. NẾU CHƯA CÓ SẢN PHẨM -> LẤY NGẪU NHIÊN (RANDOM)
            if (products.Count == 0)
            {
                // SỬA LỖI 2: Dùng AppendStage thay vì .Sample()
                var sampleStage = new BsonDocument("$sample", new BsonDocument("size", 10));

                products = await _productCollection.Aggregate()
                                                   .AppendStage<Product>(sampleStage) 
                                                   .ToListAsync();
            }

            // Mapping
            var result = new List<TopProductViewModel>();
            foreach (var p in products)
            {
                decimal gia = 0;
                if (p.Skus != null && p.Skus.Count > 0) gia = p.Skus[0].GiaBan;

                string anh = "/images/no-image.png";
                if (p.HinhAnh != null && p.HinhAnh.Count > 0) anh = p.HinhAnh[0];
                else if (p.Skus != null && p.Skus.Count > 0 && p.Skus[0].HinhAnh.Count > 0) anh = p.Skus[0].HinhAnh[0];

                result.Add(new TopProductViewModel
                {
                    ProductId = p.Id,
                    TenSanPham = p.Ten,
                    Gia = gia,
                    HinhAnh = anh,
                    DaBan = 0 
                });
            }

            return result;
        }

        public IActionResult Privacy() => View();
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}