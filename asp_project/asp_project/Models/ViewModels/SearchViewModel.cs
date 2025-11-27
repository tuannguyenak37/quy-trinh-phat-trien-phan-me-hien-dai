// /ViewModels/SearchViewModel.cs
using asp_project.Models;
using System.Collections.Generic;

namespace asp_project.ViewModels
{
    public class SearchViewModel
    {
        public string SearchQuery { get; set; } = string.Empty;
        public List<Product> Results { get; set; } = new List<Product>();
    }
}