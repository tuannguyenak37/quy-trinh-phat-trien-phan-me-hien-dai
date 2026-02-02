using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using asp_project.Models;
using asp_project.Models.ViewModels;
using asp_project.ViewModels;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace asp_project.Controllers
{
    [Authorize(Roles = "shop")]
    public class ProductsController : Controller
    {
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(IMongoDatabase database, IWebHostEnvironment webHostEnvironment)
        {
            _productsCollection = database.GetCollection<Product>("Products");
            _webHostEnvironment = webHostEnvironment;
        }

        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var shopId = GetCurrentShopId();
            if (shopId == null)
                return Forbid();

            var products = await _productsCollection.Find(p => p.ShopId == shopId).ToListAsync();

            var viewModels = products.Select(p => new ProductSummaryViewModel
            {
                Id = p.Id,
                Ten = p.Ten,
                FirstImage = (p.HinhAnh != null && p.HinhAnh.Count > 0) ? p.HinhAnh[0] : "/images/default.jpg",
                Price = (p.Skus != null && p.Skus.Count > 0) ? p.Skus[0].GiaBan : 0

            }).ToList();

            return View(viewModels);
        }

        // -------------------------------
        // 2. HIỂN THỊ FORM TẠO SẢN PHẨM
        // -------------------------------
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateProductViewModel();
            return View(model);
        }

        // -------------------------------
        // 3. XỬ LÝ SUBMIT FORM
        // -------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var shopId = GetCurrentShopId();
            if (shopId == null)
                return Forbid();

            // 1. Lưu ảnh
            var imageUrls = new List<string>();
            if (model.UploadedImages != null && model.UploadedImages.Count > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadDir);

                foreach (var imageFile in model.UploadedImages)
                {
                    if (imageFile.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        string filePath = Path.Combine(uploadDir, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        imageUrls.Add("/uploads/products/" + uniqueFileName);
                    }
                }
            }

            // 2. Tạo object Product
            var newProduct = new Product
            {
                Ten = model.Ten,
                MoTa = model.MoTa,
                DanhMuc = model.DanhMuc,
                TuyChon = model.TuyChon,
                Skus = model.Skus,
                ThuocTinh = model.ThuocTinh,
                HinhAnh = imageUrls,
                IsVisible = model.IsVisible,
                ShopId = shopId,
                CreatedAt = DateTime.UtcNow
            };

            await _productsCollection.InsertOneAsync(newProduct);
            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
            return RedirectToAction("Index");
        }
         // 4. HIỂN THỊ FORM CHỈNH SỬA (GET)
        // -------------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(id) || shopId == null)
            {
                return BadRequest();
            }

            // (Bảo mật) Chỉ tìm sản phẩm thuộc shop này
            var product = await _productsCollection.Find(p => p.Id == id && p.ShopId == shopId).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound();
            }

            // Map từ Model (Product) sang ViewModel (EditProductViewModel)
            var viewModel = new EditProductViewModel
            {
                Id = product.Id,
                Ten = product.Ten,
                MoTa = product.MoTa,
                DanhMuc = product.DanhMuc,
                TuyChon = product.TuyChon,
                Skus = product.Skus,
                ThuocTinh = product.ThuocTinh,
                IsVisible = product.IsVisible,
                ExistingImageUrls = product.HinhAnh // Tải ảnh cũ
            };

            return View(viewModel);
        }

        // -------------------------------------
        // 5. XỬ LÝ SUBMIT FORM CHỈNH SỬA (POST)
        // -------------------------------------
       // -------------------------------------
        // 5. XỬ LÝ SUBMIT FORM CHỈNH SỬA (POST) - (BẢN NÂNG CẤP)
        // -------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProductViewModel model)
        {
            
            if (ModelState.ContainsKey(nameof(model.UploadedImages)))
            {
                ModelState.Remove(nameof(model.UploadedImages));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var shopId = GetCurrentShopId();
            if (shopId == null) return Forbid();

            // (Bảo mật) Filter
            var filter = Builders<Product>.Filter.Eq(p => p.Id, model.Id) &
                         Builders<Product>.Filter.Eq(p => p.ShopId, shopId);

            // --- (MỚI) BƯỚC 1: Lấy sản phẩm GỐC từ DB để so sánh ảnh ---
            var originalProduct = await _productsCollection.Find(filter).FirstOrDefaultAsync();
            if (originalProduct == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return View(model);
            }
            var originalImageUrls = originalProduct.HinhAnh ?? new List<string>();

            // --- BƯỚC 2: Xử lý hình ảnh ---
            // Bắt đầu với danh sách ảnh mà user "tick" giữ lại
            var keptImageUrls = model.ExistingImageUrls ?? new List<string>();

            // --- (MỚI) BƯỚC 3: Tìm và Xóa các file ảnh bị bỏ tick ---
            // So sánh (ảnh gốc) VÀ (ảnh user giữ lại)
            var imagesToDelete = originalImageUrls.Except(keptImageUrls).ToList();

            if (imagesToDelete.Any())
            {
                foreach (var imageUrl in imagesToDelete)
                {
                    try
                    {
                        string fileName = Path.GetFileName(imageUrl);
                        string physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products", fileName);

                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                    catch (Exception) { /* Bỏ qua lỗi nếu file không tồn tại */ }
                }
            }
            
            // --- BƯỚC 4: Thêm ảnh mới (nếu có) ---
            // Bắt đầu danh sách cuối cùng với các ảnh đã giữ
            var finalImageUrls = new List<string>(keptImageUrls); 

            if (model.UploadedImages != null && model.UploadedImages.Count > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadDir);

                foreach (var imageFile in model.UploadedImages)
                {
                    if (imageFile.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        string filePath = Path.Combine(uploadDir, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        finalImageUrls.Add("/uploads/products/" + uniqueFileName); // Thêm ảnh mới vào danh sách
                    }
                }
            }

            // --- BƯỚC 5: Tạo lệnh Cập nhật ---
            var update = Builders<Product>.Update
                .Set(p => p.Ten, model.Ten)
                .Set(p => p.MoTa, model.MoTa)
                .Set(p => p.DanhMuc, model.DanhMuc)
                .Set(p => p.TuyChon, model.TuyChon)
                .Set(p => p.Skus, model.Skus)
                .Set(p => p.ThuocTinh, model.ThuocTinh)
                .Set(p => p.HinhAnh, finalImageUrls) // Cập nhật DB với danh sách ảnh CUỐI CÙNG
                .Set(p => p.IsVisible, model.IsVisible);

            // --- BƯỚC 6: Thực thi ---
            var result = await _productsCollection.UpdateOneAsync(filter, update);

            // (MỚI) Kiểm tra xem có thực sự thay đổi gì không
            bool noNewUploads = (model.UploadedImages == null || !model.UploadedImages.Any());
            if (result.ModifiedCount == 0 && !imagesToDelete.Any() && noNewUploads)
            {
                TempData["ErrorMessage"] = "Bạn không thay đổi gì.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        // -------------------------------------
        // 6. XỬ LÝ XÓA SẢN PHẨM (POST)
        // -------------------------------------
       // -------------------------------------
        // 6. XỬ LÝ XÓA SẢN PHẨM (POST) - (BẢN NÂNG CẤP)
        // -------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var shopId = GetCurrentShopId();
            if (string.IsNullOrEmpty(id) || shopId == null)
            {
                return BadRequest();
            }

            // (Bảo mật) Chỉ tìm sản phẩm thuộc shop này
            var filter = Builders<Product>.Filter.Eq(p => p.Id, id) &
                         Builders<Product>.Filter.Eq(p => p.ShopId, shopId);

            // --- (MỚI) BƯỚC 1: Tìm sản phẩm để lấy danh sách ảnh ---
            var productToDelete = await _productsCollection.Find(filter).FirstOrDefaultAsync();

            if (productToDelete == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để xóa.";
                return RedirectToAction("Index");
            }

            // --- (MỚI) BƯỚC 2: Xóa file vật lý trên server ---
            if (productToDelete.HinhAnh != null && productToDelete.HinhAnh.Count > 0)
            {
                foreach (var imageUrl in productToDelete.HinhAnh)
                {
                    try
                    {
                        // imageUrl có dạng "/uploads/products/ten_file.jpg"
                        string fileName = Path.GetFileName(imageUrl);
                        string physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products", fileName);
                        
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                    catch (Exception)
                    {
                        // Bỏ qua lỗi (ví dụ: file không tìm thấy) 
                        // và tiếp tục xóa khỏi DB
                    }
                }
            }

            // --- BƯỚC 3: Xóa sản phẩm khỏi Database ---
            var result = await _productsCollection.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                TempData["SuccessMessage"] = "Đã xóa sản phẩm và các ảnh liên quan thành công!";
            }
            else
            {
                // Trường hợp này gần như không xảy ra vì đã check ở Bước 1
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để xóa.";
            }

            return RedirectToAction("Index");
        }

        private string? GetCurrentShopId()
        {
            return HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ShopId")?.Value;
        }
    }
}
