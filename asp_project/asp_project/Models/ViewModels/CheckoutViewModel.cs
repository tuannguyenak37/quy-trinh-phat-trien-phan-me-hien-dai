// Trong /ViewModels/CheckoutViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace asp_project.ViewModels
{
    // Đây là đối tượng mà JavaScript sẽ gửi lên
    public class CheckoutViewModel
    {
        // 1. Thông tin khách hàng (cho khách)
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        
        // 2. ID địa chỉ (cho user đã đăng nhập)
        public string? SelectedAddressId { get; set; }
        
        // 3. Thông tin đơn hàng
        [Required]
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }

        // 4. Danh sách sản phẩm (Client gửi lên)
        [Required]
        public List<CartItemFromClient> Items { get; set; }
    }

    // Dữ liệu tối thiểu cần từ client để xác thực
    public class CartItemFromClient
    {
        [Required]
        public string ProductId { get; set; }
        
        public string SkuCode { get; set; }
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}