using Microsoft.AspNetCore.Mvc;

namespace asp_project.Controllers
{
    public class CartController : Controller
    {
        
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}