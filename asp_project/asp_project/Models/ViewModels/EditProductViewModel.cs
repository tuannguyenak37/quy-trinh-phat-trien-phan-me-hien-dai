using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using asp_project.Models;

namespace asp_project.ViewModels
{
    public class EditProductViewModel : CreateProductViewModel
    {
        public string Id { get; set; } = null!;
        public List<string> ExistingImageUrls { get; set; } = new List<string>();

        // (SỬA Ở ĐÂY)
        // Dùng 'new' để "che" thuộc tính của class cha.
        // Dùng '?' (List<IFormFile>?) để cho phép nó được null.
        // Bằng cách này, chúng ta đã ghi đè validation [Required] của class cha.
        public new List<IFormFile>? UploadedImages { get; set; }
    }
}