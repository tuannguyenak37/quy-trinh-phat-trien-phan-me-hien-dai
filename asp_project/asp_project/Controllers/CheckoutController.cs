// /Controllers/CheckoutController.cs
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using asp_project.Models;
using asp_project.ViewModels; 
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; 
using System; 
using MongoDB.Bson;
using Microsoft.Extensions.Logging; 

namespace asp_project.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IMongoCollection<Address> _addressesCollection;
        private readonly IMongoCollection<Product> _productsCollection; 
        private readonly IMongoCollection<Order> _ordersCollection; 
        private readonly IMongoDatabase _database; 
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(IMongoDatabase database, ILogger<CheckoutController> logger)
        {
            _database = database; 
            _addressesCollection = database.GetCollection<Address>("Addresses");
            _productsCollection = database.GetCollection<Product>("Products"); 
            _ordersCollection = database.GetCollection<Order>("Orders"); 
            _logger = logger; 
        }

        private string? GetCurrentUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userAddresses = new List<Address>();
            var userId = GetCurrentUserId(); 
            
            if (!string.IsNullOrEmpty(userId))
            {
                userAddresses = await _addressesCollection
                    .Find(a => a.UserId == userId && a.IsHidden == false)
                    .SortByDescending(a => a.IsDefault)
                    .ToListAsync();
            }
            
            return View(userAddresses); 
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] CheckoutViewModel model)
        {
            _logger.LogInformation("--- Nhận được yêu cầu PlaceOrder ---");
            
            if (!ModelState.IsValid)
            {
                 var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();
                
                string errorMessage = "Dữ liệu gửi lên không hợp lệ: " + string.Join(" | ", errors);
                _logger.LogWarning(errorMessage); 
                return BadRequest(new { success = false, message = errorMessage });
            }

            try
            {
                var newOrder = new Order();
                var userId = GetCurrentUserId(); 
                
                if (!string.IsNullOrEmpty(userId)) 
                    newOrder.UserId = userId;

                // --- BƯỚC 1: XÁC ĐỊNH THÔNG TIN GIAO HÀNG ---
                 if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(model.SelectedAddressId))
                {
                    var address = await _addressesCollection.Find(a => a.Id == model.SelectedAddressId && a.UserId == userId).FirstOrDefaultAsync();
                    if (address == null) 
                        return BadRequest(new { success = false, message = "Địa chỉ không hợp lệ." });
                    
                    newOrder.CustomerName = address.FullName;
                    newOrder.CustomerPhone = address.PhoneNumber;
                    newOrder.CustomerAddress = address.StreetAddress;
                }
                else
                {
                    if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.PhoneNumber) || string.IsNullOrEmpty(model.Address))
                    {
                        return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin giao hàng." });
                    }
                    newOrder.CustomerName = model.FullName;
                    newOrder.CustomerPhone = model.PhoneNumber;
                    newOrder.CustomerAddress = model.Address;
                }

                // --- BƯỚC 2: TỔNG HỢP (GROUP BY) GIỎ HÀNG ---
                 if (model.Items == null || !model.Items.Any())
                {
                    _logger.LogWarning("Giỏ hàng rỗng (model.Items is null or empty).");
                    return BadRequest(new { success = false, message = "Giỏ hàng rỗng." });
                }

                var groupedItems = model.Items
                    .GroupBy(item => new { item.ProductId, item.SkuCode })
                    .Select(g => new {
                        g.Key.ProductId,
                        g.Key.SkuCode,
                        TotalQuantity = g.Sum(item => item.Quantity) 
                    });

                var orderItems = new List<OrderItem>();
                var shopIdsInOrder = new HashSet<string>();
                var stockUpdateOperations = new List<WriteModel<Product>>();

                // --- BƯỚC 3: KIỂM TRA KHO (DÙNG LIST ĐÃ NHÓM) ---
                foreach (var groupedItem in groupedItems)
                {
                    var product = await _productsCollection.Find(p => p.Id == groupedItem.ProductId).FirstOrDefaultAsync();
                    if (product == null) throw new Exception($"Sản phẩm ID {groupedItem.ProductId} không tồn tại.");
                    
                    var sku = product.Skus.FirstOrDefault(s => s.SkuCode == groupedItem.SkuCode);
                    if (sku == null) throw new Exception($"SKU {groupedItem.SkuCode} không hợp lệ.");

                    _logger.LogInformation($"Kiểm tra kho cho SKU: {sku.SkuCode} - Kho: {sku.TonKho} - Cần: {groupedItem.TotalQuantity}");
                    
                    if (sku.TonKho < groupedItem.TotalQuantity)
                    {
                        throw new Exception($"Sản phẩm {product.Ten} - {sku.SkuCode} không đủ hàng (Bạn cần {groupedItem.TotalQuantity} nhưng chỉ còn {sku.TonKho}).");
                    }
                    
                    // --- (PHẦN MỚI: Lấy ảnh) ---
                    // Ưu tiên ảnh của SKU (biến thể, ví dụ: ảnh áo màu đỏ)
                    string imageUrl = sku.HinhAnh.FirstOrDefault(); 
                    
                    // Nếu SKU không có ảnh riêng, lấy ảnh chung của sản phẩm
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        imageUrl = product.HinhAnh.FirstOrDefault();
                    }
                

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        SkuCode = sku.SkuCode,
                        ProductName = product.Ten, 
                        VariantName = string.Join(" / ", sku.GiaTriTuyChon.Select(v => v.GiaTri)),
                        Quantity = groupedItem.TotalQuantity, 
                        Price = sku.GiaBan,
                        ShopId = product.ShopId,
                        ImagePath = imageUrl ?? string.Empty // <-- (THÊM VÀO)
                    });
                    
                    shopIdsInOrder.Add(product.ShopId);
                    
                    var filter = Builders<Product>.Filter.And(
                        Builders<Product>.Filter.Eq(p => p.Id, product.Id),
                        Builders<Product>.Filter.ElemMatch(p => p.Skus, s => s.SkuCode == groupedItem.SkuCode && s.TonKho >= groupedItem.TotalQuantity)
                    );
                    
                    var update = Builders<Product>.Update.Inc("Skus.$[elem].tonKho", -groupedItem.TotalQuantity); 
                    var arrayFilters = new List<ArrayFilterDefinition<Product>> { 
                        new BsonDocument("elem.skuCode", groupedItem.SkuCode) 
                    };
                    stockUpdateOperations.Add(new UpdateOneModel<Product>(filter, update) { ArrayFilters = arrayFilters });
                }

                // --- BƯỚC 4: THỰC HIỆN TRỪ KHO ---
                _logger.LogInformation($"Chuẩn bị chạy BulkWriteAsync với {stockUpdateOperations.Count} lệnh.");
                var updateResult = await _productsCollection.BulkWriteAsync(stockUpdateOperations);
                _logger.LogInformation($"BulkWriteAsync hoàn tất. ModifiedCount: {updateResult.ModifiedCount} / Expected: {stockUpdateOperations.Count}");

                if (updateResult.ModifiedCount < stockUpdateOperations.Count)
                {
                    throw new Exception("Không đủ hàng tồn kho cho một hoặc nhiều sản phẩm (đã bị người khác mua).");
                }

                // --- BƯỚC 5: TẠO ĐƠN HÀNG ---
                newOrder.Items = orderItems;
                newOrder.TotalAmount = orderItems.Sum(item => item.Price * item.Quantity);
                newOrder.PaymentMethod = model.PaymentMethod;
                newOrder.Notes = model.Notes ?? "";
                newOrder.ShopIds = shopIdsInOrder.ToList();
                newOrder.Status = "Pending";

                await _ordersCollection.InsertOneAsync(newOrder);

                // --- BƯỚC 6: TRẢ VỀ KẾT QUẢ ---
                _logger.LogInformation($"--- Đặt hàng THÀNH CÔNG! OrderId: {newOrder.Id} ---");
                return Ok(new { success = true, orderId = newOrder.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"--- LỖI 400 KHI ĐẶT HÀNG: {ex.Message} ---");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}