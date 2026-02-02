using System.Collections.Generic;
using asp_project.Models;

namespace asp_project.Models.ViewModels
{
    public class ProductPageDetailViewModel
    {
        public Product Product { get; set; }
        public Shop Shop { get; set; }
        public List<Product> RelatedProducts { get; set; }
    }
}
