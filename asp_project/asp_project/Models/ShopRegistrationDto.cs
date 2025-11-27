using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace asp_project.Dtos
{
    public class ShopRegistrationDto
    {
        public string ShopName { get; set; }
        public string Phone { get; set; }
        public string CCCD { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public IFormFile? AvatarFile { get; set; } // Upload ảnh
        public List<string>? CategoryIds { get; set; } // Danh mục kinh doanh
    }
}
