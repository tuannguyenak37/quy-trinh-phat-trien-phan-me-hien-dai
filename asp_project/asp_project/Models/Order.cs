using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System;

namespace asp_project.Models
{
    [BsonIgnoreExtraElements]
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        // Map chữ hoa C# sang chữ thường MongoDB
        [BsonElement("customerName")] 
        public string CustomerName { get; set; } = string.Empty;

        [BsonElement("customerPhone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [BsonElement("customerAddress")]
        public string CustomerAddress { get; set; } = string.Empty;
        
        [BsonElement("userId")]
        public string? UserId { get; set; } 

        // QUAN TRỌNG: Map "Items" sang "items"
        [BsonElement("items")] 
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        
        [BsonElement("totalAmount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalAmount { get; set; } 
        
        [BsonElement("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty;

        [BsonElement("notes")]
        public string Notes { get; set; } = string.Empty;
        
        [BsonElement("shopIds")]
        public List<string> ShopIds { get; set; } = new List<string>();

        [BsonElement("status")]
        public string Status { get; set; } = "Pending"; 
        
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [BsonIgnoreExtraElements]
    public class OrderItem
    {
        [BsonElement("productId")]
        public string ProductId { get; set; } = string.Empty;

        [BsonElement("skuCode")]
        public string SkuCode { get; set; } = string.Empty;

        [BsonElement("productName")]
        public string ProductName { get; set; } = string.Empty;

        [BsonElement("variantName")]
        public string VariantName { get; set; } = string.Empty;

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; } 
        
        [BsonElement("imagePath")]
        public string ImagePath { get; set; } = string.Empty;

        [BsonElement("shopId")]
        public string ShopId { get; set; } = string.Empty; 
    }
}