using Microsoft.AspNetCore.Mvc;

namespace asp_project.Controllers
{
    public class CartController : Controller
    {
        // Trang Index() này chỉ đơn giản là trả về cái View.
        // Toàn bộ logic sẽ nằm trong file JavaScript của View.
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}