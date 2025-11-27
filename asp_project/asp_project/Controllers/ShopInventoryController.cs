// Trong /Controllers/ShopInventoryController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using asp_project.Models;
using asp_project.ViewModels; 
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // Để Log
using System.IO; // (MỚI) Cần cho MemoryStream
using ClosedXML.Excel; // (MỚI) Thư viện Excel

namespace asp_project.Controllers
{
    [Authorize(Roles = "shop")] 
    public class ShopInventoryController : Controller
    {
        private readonly IMongoCollection<Product> _productsCollection;

        public ShopInventoryController(IMongoDatabase database)
        {
            _productsCollection = database.GetCollection<Product>("Products");
        }

        private string? GetCurrentShopId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == "ShopId")?.Value;
        }

        // -------------------------------------------------
        // 1. HIỂN THỊ DANH SÁCH (Phần này đã đúng)
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized();

            // (SỬA LỖI FORMAT EXCEPTION)
            // Lỗi này xảy ra do C# (Skus) và DB (skus) không khớp.
            // Chúng ta đã sửa lỗi này trong Program.cs bằng CamelCaseConvention.
            var products = await _productsCollection.Find(p => p.ShopId == shopId).ToListAsync();

            var viewModel = new ShopInventoryViewModel();
            foreach (var product in products)
            {
                var productVM = new ProductStockUpdateModel
                {
                    ProductId = product.Id,
                    ProductName = product.Ten,
                };

                var mainProductImage = (product.HinhAnh != null && product.HinhAnh.Count > 0) 
                                        ? product.HinhAnh[0] 
                                        : "/images/default.jpg"; 

                foreach (var sku in product.Skus)
                {
                    string imageUrl = (sku.HinhAnh != null && sku.HinhAnh.Count > 0)
                                      ? sku.HinhAnh[0]
                                      : mainProductImage; 

                    var skuNameString = string.Join(" / ", sku.GiaTriTuyChon.Select(opt => $"{opt.Ten}: {opt.GiaTri}"));
                    
                    productVM.Skus.Add(new SkuStockUpdateModel
                    {
                        SkuCode = sku.SkuCode, 
                        SkuName = string.IsNullOrEmpty(skuNameString) ? "Mặc định" : skuNameString,
                        TonKho = sku.TonKho,
                        ImageUrl = imageUrl, 
                        SoLuongThem = 0 
                    });
                }
                viewModel.Products.Add(productVM);
            }

            return View(viewModel);
        }

        // -------------------------------------------------
        // 2. XỬ LÝ NHẬP KHO (ĐÃ SỬA LỖI camelCase)
        // -------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromBody] ShopInventoryViewModel model) 
        {
            try
            {
                string jsonData = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
                System.Diagnostics.Debug.WriteLine("--- DỮ LIỆU JSON (SẠCH) NHẬN ĐƯỢC: ---");
                System.Diagnostics.Debug.WriteLine(jsonData);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI NGHIÊM TRỌNG] Không thể serialize model: {ex.Message}");
            }

            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized();

            var bulkUpdates = new List<WriteModel<Product>>();

            System.Diagnostics.Debug.WriteLine($"--- BẮT ĐẦU NHẬP KHO CHO SHOP: {shopId} ---");

            foreach (var productVM in model.Products)
            {
                foreach (var skuVM in productVM.Skus)
                {
                    if (skuVM.SoLuongThem > 0)
                    {
                        // Sửa lỗi null thành chuỗi rỗng
                        string skuCodeToMatch = skuVM.SkuCode ?? "";

                        System.Diagnostics.Debug.WriteLine(
                            $"[LOG] Sẵn sàng cập nhật: ProductId={productVM.ProductId}, SkuCodeToMatch='{skuCodeToMatch}', Thêm='{skuVM.SoLuongThem}'"
                        );

                        // (ĐÚNG) Dùng ElemMatch và C# PascalCase (tự động chuyển)
                        var filter = Builders<Product>.Filter.Eq(p => p.Id, productVM.ProductId) &
                                     Builders<Product>.Filter.Eq(p => p.ShopId, shopId) &
                                     Builders<Product>.Filter.ElemMatch(p => p.Skus, sku => sku.SkuCode == skuCodeToMatch);

                        // (ĐÚNG) Dùng camelCase 'skus.$.tonKho' cho chuỗi BSON
                        var update = Builders<Product>.Update.Inc("skus.$.tonKho", skuVM.SoLuongThem);

                        bulkUpdates.Add(new UpdateOneModel<Product>(filter, update));
                    }
                }
            }

            if (bulkUpdates.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[LOG] Đã chuẩn bị {bulkUpdates.Count} lệnh. Gửi BulkWriteAsync...");
                
                var result = await _productsCollection.BulkWriteAsync(bulkUpdates);

                long matchedCount = result.MatchedCount;   
                long modifiedCount = result.ModifiedCount; 
                
                System.Diagnostics.Debug.WriteLine(
                    $"[LOG] KẾT QUẢ TỪ MONGODB: Matched={matchedCount}, Modified={modifiedCount}"
                );

                if (modifiedCount > 0)
                {
                    TempData["SuccessMessage"] = $"Đã nhập kho thành công! ({modifiedCount} phân loại đã được cập nhật).";
                }
                else if (matchedCount == 0)
                {
                    TempData["ErrorMessage"] = "LỖI: Không tìm thấy sản phẩm/phân loại nào khớp. (Filter Matched=0).";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi: Đã tìm thấy sản phẩm nhưng không thể cập nhật (Modified=0).";
                }
            }
            else
            {
                TempData["InfoMessage"] = "Bạn không nhập số lượng 'Thêm' nào, nên không có gì thay đổi.";
            }
            
            System.Diagnostics.Debug.WriteLine("--- KẾT THÚC NHẬP KHO ---");
            
            // Trả về JSON cho JavaScript (vì chúng ta dùng Fetch)
            if (TempData["SuccessMessage"] != null)
                return Ok(new { success = true, message = TempData["SuccessMessage"] });
            
            return BadRequest(new { success = false, message = TempData["ErrorMessage"] ?? TempData["InfoMessage"] });
        }

        // -------------------------------------------------
        // 3. (MỚI) CHỨC NĂNG XUẤT EXCEL
        // -------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized();

            // 1. Lấy tất cả sản phẩm của shop
            var products = await _productsCollection.Find(p => p.ShopId == shopId).ToListAsync();

            // 2. Tạo file Excel
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("TonKho");
                var currentRow = 1;

                // 3. Tạo tiêu đề
                worksheet.Cell(currentRow, 1).Value = "Tên sản phẩm";
                worksheet.Cell(currentRow, 2).Value = "Phân loại";
                worksheet.Cell(currentRow, 3).Value = "Mã Phân loại (SKU)";
                worksheet.Cell(currentRow, 4).Value = "Tồn kho (cái)";
                
                // (Tô đậm tiêu đề)
                worksheet.Row(currentRow).Style.Font.Bold = true;

                // 4. Lặp qua dữ liệu và thêm vào file
                foreach (var product in products)
                {
                    foreach (var sku in product.Skus)
                    {
                        currentRow++;
                        
                        var skuNameString = string.Join(" / ", sku.GiaTriTuyChon.Select(opt => $"{opt.Ten}: {opt.GiaTri}"));
                        
                        worksheet.Cell(currentRow, 1).Value = product.Ten;
                        worksheet.Cell(currentRow, 2).Value = string.IsNullOrEmpty(skuNameString) ? "Mặc định" : skuNameString;
                        worksheet.Cell(currentRow, 3).Value = sku.SkuCode;
                        worksheet.Cell(currentRow, 4).Value = sku.TonKho;
                    }
                }

                // 5. Căn chỉnh cột
                worksheet.Columns().AdjustToContents();

                // 6. Lưu vào MemoryStream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    
                    // 7. Trả về File
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"TonKho_Shop_{DateTime.Now:ddMMyyyy}.xlsx");
                }
            }
        }
    }
}