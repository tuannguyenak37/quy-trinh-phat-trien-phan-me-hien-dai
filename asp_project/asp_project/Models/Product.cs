using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace asp_project.Models
{
    [BsonIgnoreExtraElements] // Bỏ qua các trường thừa để tránh lỗi Crash
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("shopId")] // Map với "shopId" trong DB
        public string ShopId { get; set; } = string.Empty;

        [BsonElement("ten")] // Map với "ten"
        public string Ten { get; set; } = string.Empty;

        [BsonElement("moTa")]
        public string MoTa { get; set; } = string.Empty;

        [BsonElement("danhMuc")]
        public List<string> DanhMuc { get; set; } = new List<string>();

        [BsonElement("hinhAnh")] // Map với "hinhAnh"
        public List<string> HinhAnh { get; set; } = new List<string>();

        [BsonElement("tuyChon")]
        public List<ProductOption> TuyChon { get; set; } = new List<ProductOption>();

        [BsonElement("skus")] // Map với "skus"
        public List<Sku> Skus { get; set; } = new List<Sku>();

        [BsonElement("thuocTinh")]
        public List<ProductAttribute> ThuocTinh { get; set; } = new List<ProductAttribute>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isVisible")]
        public bool IsVisible { get; set; } = false;
    }

    public class ProductOption
    {
        [BsonElement("ten")]
        public string Ten { get; set; } = string.Empty;
        [BsonElement("giaTri")]
        public List<string> GiaTri { get; set; } = new List<string>();
    }

    public class Sku
    {
        [BsonElement("skuCode")]
        public string SkuCode { get; set; } = string.Empty;

        [BsonElement("giaTriTuyChon")]
        public List<SkuOptionValue> GiaTriTuyChon { get; set; } = new List<SkuOptionValue>();

        [BsonElement("giaBan")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal GiaBan { get; set; }

        [BsonElement("tonKho")]
        public int TonKho { get; set; }

        [BsonElement("hinhAnh")]
        public List<string> HinhAnh { get; set; } = new List<string>();
    }

    public class SkuOptionValue
    {
        [BsonElement("ten")]
        public string Ten { get; set; } = string.Empty;
        [BsonElement("giaTri")]
        public string GiaTri { get; set; } = string.Empty;
    }

    public class ProductAttribute
    {
        [BsonElement("tenThuocTinh")]
        public string TenThuocTinh { get; set; } = string.Empty;
        [BsonElement("giaTri")]
        public string GiaTri { get; set; } = string.Empty;
    }
}