using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes; // <--- THÊM THƯ VIỆN NÀY
using System;
using System.ComponentModel.DataAnnotations;

namespace asp_project.Models
{
    // [QUAN TRỌNG] Dòng này giúp bỏ qua lỗi nếu DB có trường lạ/thừa
    // Nó là "bùa hộ mệnh" giúp web không bị sập vì lỗi FormatException
    [BsonIgnoreExtraElements] 
    public class User
    {
        [BsonId] // Đánh dấu khóa chính
        [BsonRepresentation(BsonType.ObjectId)] // Tự động chuyển ObjectId sang string
        public string? Id { get; set; }

        // [QUAN TRỌNG] Map chính xác tên trường trong Database
        // Lỗi báo 'FirstName', ta map đúng "FirstName"
        [BsonElement("FirstName")] 
        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(50, ErrorMessage = "Họ không được dài quá 50 ký tự")]
        public string? FirstName { get; set; }

        [BsonElement("LastName")]
        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên không được dài quá 50 ký tự")]
        public string? LastName { get; set; }

        [BsonElement("Birthday")]
        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date, ErrorMessage = "Ngày sinh không hợp lệ")]
        public DateTime Birthday { get; set; }

        [BsonElement("Role")]
        [Required(ErrorMessage = "Role là bắt buộc")]
        [RegularExpression("user|shop|admin", ErrorMessage = "Role phải là 'user', 'shop' hoặc 'admin'")]
        public string? Role { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonElement("Email")]
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [BsonElement("Password")]
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 ký tự trở lên")]
        public string? Password { get; set; } 
    }
}