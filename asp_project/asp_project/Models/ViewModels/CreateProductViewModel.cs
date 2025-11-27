using Microsoft.AspNetCore.Http; // Cần cho IFormFile
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using asp_project.Models; // Nơi chứa Sku, ProductOption...

namespace asp_project.ViewModels
{
    public class CreateProductViewModel
    {
        [Required]
        public string Ten { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;

        [Required]
        public List<string> DanhMuc { get; set; } = new List<string>();

      
        public List<IFormFile> UploadedImages { get; set; } = new List<IFormFile>();

        public List<ProductOption> TuyChon { get; set; } = new List<ProductOption>();

        [Required]
        public List<Sku> Skus { get; set; } = new List<Sku>();

        public List<ProductAttribute> ThuocTinh { get; set; } = new List<ProductAttribute>();
        public bool IsVisible { get; set; } = false;
    }
}