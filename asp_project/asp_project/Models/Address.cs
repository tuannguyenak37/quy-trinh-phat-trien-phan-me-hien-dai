// Trong /Models/Address.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace asp_project.Models
{
    public class Address
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        // Liên kết với collection Users
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        // Thông tin địa chỉ
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        // (Bạn có thể thêm Tỉnh/Thành, Quận/Huyện... sau)

        public bool IsDefault { get; set; } = false;

        // Đây là chức năng "Ẩn" (Soft Delete) của bạn
        public bool IsHidden { get; set; } = false; 
    }
}