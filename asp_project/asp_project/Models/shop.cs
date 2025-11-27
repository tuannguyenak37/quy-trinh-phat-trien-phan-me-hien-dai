using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes; // <--- Cần thư viện này
using System;
using System.Collections.Generic;

namespace asp_project.Models
{
    // [QUAN TRỌNG] Dòng này giúp bỏ qua các trường thừa, tránh lỗi FormatException
    [BsonIgnoreExtraElements] 
    public class Shop
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // Tự động xử lý ID giữa String và ObjectId
        public string Id { get; set; } = null!;

        [BsonElement("ShopName")] // Map chính xác tên trong DB
        public string ShopName { get; set; } = string.Empty;

        [BsonElement("CCCD")]
        public string CCCD { get; set; } = string.Empty;

        [BsonElement("phone")] // Map với "phone" hoặc "Phone" tùy DB
        public string Phone { get; set; } = string.Empty;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [BsonElement("address")]
        public string? Address { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("categoryIds")]
        public List<string> CategoryIds { get; set; } = new List<string>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}