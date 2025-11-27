using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using asp_project.Models;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize] 
public class AddressController : Controller
{
    private readonly IMongoCollection<Address> _addressesCollection;

    public AddressController(IMongoDatabase database)
    {
        _addressesCollection = database.GetCollection<Address>("Addresses");
    }

    private string GetCurrentUserId()
    {
        
        return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value; 
    }

    // API: TẠO địa chỉ mới (Chức năng "Tạo")
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Address model)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        model.UserId = userId;
        model.IsHidden = false; // Luôn hiển thị khi mới tạo

        await _addressesCollection.InsertOneAsync(model);
        return Ok(model); // Trả về địa chỉ vừa tạo
    }

    // API: "ẨN" địa chỉ (Chức năng "Ẩn")
    [HttpPut]
    public async Task<IActionResult> Hide(string id)
    {
        var userId = GetCurrentUserId();
        var filter = Builders<Address>.Filter.Eq(a => a.Id, id) &
                     Builders<Address>.Filter.Eq(a => a.UserId, userId);
        
        var update = Builders<Address>.Update.Set(a => a.IsHidden, true);
        
        var result = await _addressesCollection.UpdateOneAsync(filter, update);

        if (result.ModifiedCount == 0) return NotFound();
        return Ok(new { success = true });
    }
    
    // (Bạn có thể thêm các API khác như GetList, Update, SetDefault...)
}