using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using asp_project.Models;

namespace asp_project.ViewModels
{
    public class EditProductViewModel : CreateProductViewModel
    {
        public string Id { get; set; } = null!;
        public List<string> ExistingImageUrls { get; set; } = new List<string>();

        
        public new List<IFormFile>? UploadedImages { get; set; }
    }
}