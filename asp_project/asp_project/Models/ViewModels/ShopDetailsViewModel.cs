using System.Collections.Generic;
using asp_project.Models;

namespace asp_project.Models.ViewModels
{
    public class ShopDetailsViewModel
    {
        public Shop Shop { get; set; }
        public List<Product> Products { get; set; }
    }
}
