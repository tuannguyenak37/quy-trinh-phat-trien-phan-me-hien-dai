using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using asp_project.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using ClosedXML.Excel; 
using System.IO; 

namespace asp_project.Controllers
{
    [Authorize]
    public class MyStoreController : Controller
    {
        private readonly IMongoCollection<Order> _ordersCollection;
        private readonly IMongoCollection<Product> _productsCollection; 
        private readonly ILogger<MyStoreController> _logger;

        public MyStoreController(IMongoDatabase database, ILogger<MyStoreController> logger)
        {
            _ordersCollection = database.GetCollection<Order>("Orders");
            _productsCollection = database.GetCollection<Product>("Products"); 
            _logger = logger;
        }

        private string? GetCurrentShopId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == "ShopId")?.Value;
        }

        // --- TRANG 1: TỔNG QUAN (DASHBOARD) ---
        [HttpGet]
        public IActionResult Index()
        {
            // Trả về View: /Views/MyStore/Index.cshtml
            return View();
        }

        // --- API CHO TRANG 1 (TỔNG QUAN) ---
       // --- API CHO TRANG 1 (TỔNG QUAN) ---
[HttpGet]
public async Task<IActionResult> GetDashboardStats(
    [FromQuery] DateTime? startDate, 
    [FromQuery] DateTime? endDate)
{
    var shopId = GetCurrentShopId();
    if (string.IsNullOrEmpty(shopId)) return Unauthorized(new { message = "Không tìm thấy Shop ID." });
    
    var end = (endDate ?? DateTime.UtcNow.Date).AddDays(1).AddSeconds(-1); 
    var start = (startDate ?? DateTime.UtcNow.Date.AddDays(-6)).Date; 
    
    _logger.LogInformation($"Lấy thống kê ShopId: {shopId} từ {start} đến {end}");

    try
    {
        
        var chartStatsTask = _getChartStats(shopId, start, end);
        var statusStatsTask = _getStatusStats(shopId);
        var lowStockTask = _getLowStockItems(shopId);
        
        await Task.WhenAll(chartStatsTask, statusStatsTask, lowStockTask);
        
        
        var (labels, revenueData, orderData, overallRevenue, overallOrders) = _processChartStats(chartStatsTask.Result, start, end);
        
      
        var statusCounts = _processStatusStats(statusStatsTask.Result);
        
        var lowStockItems = lowStockTask.Result;

        return Ok(new { 
            labels, 
            revenueData, 
            orderData,      
            overallRevenue, 
            overallOrders,  
            statusCounts, 
            lowStockItems 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lỗi khi chạy Aggregation thống kê");
        return StatusCode(500, new { message = "Lỗi máy chủ khi lấy dữ liệu thống kê." });
    }
}

        // ==================================================================
        // --- (MỚI) TRANG 2: THỐNG KÊ (HÓA ĐƠN & EXCEL) ---
        // ==================================================================
        [HttpGet]
        public IActionResult Statistics()
        {
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyRevenueStats([FromQuery] int year)
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized(new { message = "Không tìm thấy Shop ID." });

            if (year == 0) year = DateTime.UtcNow.Year;
            _logger.LogInformation($"Lấy doanh thu tháng cho ShopId: {shopId}, Năm: {year}");
            
            var matchStage = new BsonDocument("$match", new BsonDocument
            {
                { "status", "Delivered" },
                { "createdAt", new BsonDocument { 
                    { "$gte", new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, 
                    { "$lt", new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc) } 
                }},
                { "shopIds", shopId }
            });
            var unwindStage = new BsonDocument("$unwind", "$items");
            var matchShopStage = new BsonDocument("$match", new BsonDocument("items.shopId", shopId));
            var groupStage = new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument("$month", "$createdAt") }, 
                { "totalRevenue", new BsonDocument("$sum", new BsonDocument("$multiply", new BsonArray { "$items.price", "$items.quantity" })) }
            });
            var sortStage = new BsonDocument("$sort", new BsonDocument("_id", 1));

            var results = await _ordersCollection.Aggregate()
                .AppendStage<BsonDocument>(matchStage)
                .AppendStage<BsonDocument>(unwindStage)
                .AppendStage<BsonDocument>(matchShopStage)
                .AppendStage<BsonDocument>(groupStage)
                .AppendStage<BsonDocument>(sortStage)
                .ToListAsync();

            var monthlyRevenue = new decimal[12];
            foreach (var doc in results)
            {
                int monthIndex = doc["_id"].AsInt32 - 1; 
                if (monthIndex >= 0 && monthIndex < 12)
                {
                    monthlyRevenue[monthIndex] = doc["totalRevenue"].AsDecimal;
                }
            }
            
            var labels = new[] { "Thg 1", "Thg 2", "Thg 3", "Thg 4", "Thg 5", "Thg 6", "Thg 7", "Thg 8", "Thg 9", "Thg 10", "Thg 11", "Thg 12" };
            return Ok(new { labels, revenueData = monthlyRevenue });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersByMonth([FromQuery] int year, [FromQuery] int month)
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Unauthorized(new { message = "Không tìm thấy Shop ID." });

            var orders = await _getOrdersByMonth(shopId, year, month);
            
            var simplifiedOrders = orders.Select(o => new {
                id = o.Id,
                createdAt = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                customerName = o.CustomerName,
                shopTotal = o.Items.Where(i => i.ShopId == shopId).Sum(i => i.Price * i.Quantity),
                status = o.Status
            });
            
            return Ok(simplifiedOrders);
        }

        // --- (MỚI) API CHO TRANG 2: XUẤT EXCEL ---
        [HttpGet]
        public async Task<IActionResult> ExportOrdersToExcel([FromQuery] int year, [FromQuery] int month)
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(shopId)) return Forbid(); 

            var orders = await _getOrdersByMonth(shopId, year, month);
            string fileName = $"HoaDon_Thang_{month}_{year}.xlsx";
            
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Hóa đơn");
                
                worksheet.Cell(1, 1).Value = "Mã Đơn Hàng";
                worksheet.Cell(1, 2).Value = "Ngày Tạo";
                worksheet.Cell(1, 3).Value = "Tên Khách Hàng";
                worksheet.Cell(1, 4).Value = "SĐT Khách Hàng";
                worksheet.Cell(1, 5).Value = "Địa Chỉ";
                worksheet.Cell(1, 6).Value = "Doanh Thu (của Shop)";
                worksheet.Cell(1, 7).Value = "Trạng Thái";
                worksheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (var order in orders)
                {
                    var shopTotal = order.Items.Where(i => i.ShopId == shopId).Sum(i => i.Price * i.Quantity);
                    
                    worksheet.Cell(row, 1).Value = order.Id;
                    worksheet.Cell(row, 2).Value = order.CreatedAt.ToLocalTime(); 
                    worksheet.Cell(row, 3).Value = order.CustomerName;
                    worksheet.Cell(row, 4).Value = order.CustomerPhone;
                    worksheet.Cell(row, 5).Value = order.CustomerAddress;
                    worksheet.Cell(row, 6).Value = shopTotal;
                    worksheet.Cell(row, 7).Value = order.Status;
                    row++;
                }

                worksheet.Columns().AdjustToContents(); 

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
        
        // ==================================================================
        // --- CÁC HÀM HELPER (DÙNG CHUNG) ---
        // ==================================================================

        // --- (MỚI) HÀM DÙNG CHUNG: Lấy Hóa đơn theo tháng ---
        private async Task<List<Order>> _getOrdersByMonth(string shopId, int year, int month)
        {
            if (month < 1 || month > 12) month = DateTime.UtcNow.Month;
            if (year < 2020 || year > 2100) year = DateTime.UtcNow.Year;
            
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1);

            var filter = Builders<Order>.Filter.And(
                Builders<Order>.Filter.Gte(o => o.CreatedAt, startDate),
                Builders<Order>.Filter.Lt(o => o.CreatedAt, endDate),
                Builders<Order>.Filter.AnyEq(o => o.ShopIds, shopId)
            );

            return await _ordersCollection.Find(filter).SortByDescending(o => o.CreatedAt).ToListAsync();
        }

        // --- (Helpers cho API 1, giữ nguyên) ---
        private Task<List<BsonDocument>> _getChartStats(string shopId, DateTime start, DateTime end) {
            var matchStage1 = new BsonDocument("$match", new BsonDocument { { "status", "Delivered" }, { "createdAt", new BsonDocument { { "$gte", start }, { "$lte", end } } }, { "shopIds", shopId } });
            var unwindStage = new BsonDocument("$unwind", "$items");
            var matchStage2 = new BsonDocument("$match", new BsonDocument("items.shopId", shopId));
            var groupStage1 = new BsonDocument("$group", new BsonDocument { { "_id", new BsonDocument { { "date", new BsonDocument("$dateToString", new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$createdAt" } }) }, { "orderId", "$_id" } } }, { "shopRevenueInOrder", new BsonDocument("$sum", new BsonDocument("$multiply", new BsonArray { "$items.price", "$items.quantity" })) } });
            var groupStage2 = new BsonDocument("$group", new BsonDocument { { "_id", "$_id.date" }, { "totalRevenue", new BsonDocument("$sum", "$shopRevenueInOrder") }, { "totalOrders", new BsonDocument("$sum", 1) } });
            var sortStage = new BsonDocument("$sort", new BsonDocument("_id", 1));
            return _ordersCollection.Aggregate().AppendStage<BsonDocument>(matchStage1).AppendStage<BsonDocument>(unwindStage).AppendStage<BsonDocument>(matchStage2).AppendStage<BsonDocument>(groupStage1).AppendStage<BsonDocument>(groupStage2).AppendStage<BsonDocument>(sortStage).ToListAsync();
        }
        
        private Task<List<BsonDocument>> _getStatusStats(string shopId) {
            var matchStatusStage = new BsonDocument("$match", new BsonDocument("shopIds", shopId));
            var groupStatusStage = new BsonDocument("$group", new BsonDocument { { "_id", "$status" }, { "count", new BsonDocument("$sum", 1) } });
            return _ordersCollection.Aggregate().AppendStage<BsonDocument>(matchStatusStage).AppendStage<BsonDocument>(groupStatusStage).ToListAsync();
        }
        
        private Task<List<LowStockItem>> _getLowStockItems(string shopId) {
             var lowStockMatchShop = new BsonDocument("$match", new BsonDocument("shopId", shopId));
             var lowStockUnwind = new BsonDocument("$unwind", "$skus");
             var lowStockMatchStock = new BsonDocument("$match", new BsonDocument("skus.tonKho", new BsonDocument("$lt", 5))); 
             var lowStockProject = new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "productId", new BsonDocument("$toString", "$_id") }, { "productName", "$ten" }, { "skuCode", "$skus.skuCode" }, { "stockLeft", "$skus.tonKho" } });
             return _productsCollection.Aggregate().AppendStage<BsonDocument>(lowStockMatchShop).AppendStage<BsonDocument>(lowStockUnwind).AppendStage<BsonDocument>(lowStockMatchStock).AppendStage<BsonDocument>(lowStockProject).As<LowStockItem>().ToListAsync();
        }
        
       // Sửa kiểu trả về của hàm: Thêm List<int> vào vị trí thứ 3
private (List<string>, List<decimal>, List<int>, decimal, int) _processChartStats(List<BsonDocument> chartStats, DateTime start, DateTime end) 
{
    decimal overallRevenue = chartStats.Sum(s => s["totalRevenue"].AsDecimal);
    int overallOrders = chartStats.Sum(s => s["totalOrders"].AsInt32);
    
    var labels = new List<string>();
    var revenueData = new List<decimal>();
    var orderData = new List<int>(); // List chứa số đơn hàng theo ngày

    // Chuyển List thành Dictionary để tra cứu nhanh theo ngày
    var statsDict = chartStats.ToDictionary(
        s => s["_id"].AsString, 
        s => new { 
            Revenue = s["totalRevenue"].AsDecimal, 
            Orders = s["totalOrders"].AsInt32 
        }
    );

    // Vòng lặp để điền dữ liệu (kể cả những ngày không có đơn cũng phải điền số 0)
    for (var day = start.Date; day <= end.Date; day = day.AddDays(1)) 
    {
        var dateString = day.ToString("yyyy-MM-dd");
        labels.Add(day.ToString("dd/MM"));

        if (statsDict.TryGetValue(dateString, out var dayStat)) 
        { 
            revenueData.Add(dayStat.Revenue);
            orderData.Add(dayStat.Orders); // <--- Thêm dữ liệu vào list
        }
        else 
        { 
            revenueData.Add(0);
            orderData.Add(0); // <--- Điền 0 nếu không có đơn
        }
    }
    
    // Trả về đầy đủ 5 tham số
    return (labels, revenueData, orderData, overallRevenue, overallOrders); 
}

    private Dictionary<string, int> _processStatusStats(List<BsonDocument> statusStats) 
{

    var result = new Dictionary<string, int> {
        { "Pending", 0 },
        { "Shipping", 0 },
        { "Delivered", 0 },
        { "Cancelled", 0 } 
    };

    foreach (var stat in statusStats)
    {
        string dbStatus = stat["_id"].AsString; 
        int count = stat["count"].AsInt32;

        string uiKey = dbStatus;
        
        
        if (dbStatus == "Canceled") uiKey = "Cancelled";
        if (dbStatus == "Cancled") uiKey = "Cancelled"; 

        if (result.ContainsKey(uiKey))
        {
            result[uiKey] = count;
        }
        else
        {
          
            result[uiKey] = count;
        }
    }

    return result;
}
        
        private class LowStockItem {
            [BsonElement("productId")] public string ProductId { get; set; }
            [BsonElement("productName")] public string ProductName { get; set; }
            [BsonElement("skuCode")] public string SkuCode { get; set; }
            [BsonElement("stockLeft")] public int StockLeft { get; set; }
        }
    }
}