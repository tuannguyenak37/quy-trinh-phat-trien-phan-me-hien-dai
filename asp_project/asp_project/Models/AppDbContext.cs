using asp_project.Controllers;
using MongoFramework;

namespace asp_project.Models  
{
    public class AppDbContext : MongoDbContext
    {
        public MongoDbSet<User> Users { get; set; } // <--- THÊM DÒNG NÀY
        public MongoDbSet<Shop> Shops { get; set; } = null!;
        public MongoDbSet<Product> Products { get; set; } = null!;
        public AppDbContext(IMongoDbConnection connection) : base(connection) { }
    }
}
