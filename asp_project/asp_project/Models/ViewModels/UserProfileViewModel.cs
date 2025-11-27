using System;
using System.ComponentModel.DataAnnotations;

namespace asp_project.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; } // Thường email không cho sửa hoặc readonly

        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Họ")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Tên")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime Birthday { get; set; }
        
        public string Role { get; set; } // Chỉ để hiển thị, không bind lại để sửa
    }
}