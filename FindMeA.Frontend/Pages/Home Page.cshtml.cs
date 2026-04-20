using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Find_Me_A; // backend namespace

namespace FindMeA.Frontend.Pages
{
    public class HomePageModel : PageModel
    {
        private readonly IConfiguration _config;

        public HomePageModel(IConfiguration config)
        {
            _config = config;
        }

        public void OnGet()
        {
            // Later you can load recommendations here
            // string connectionString = _config.GetConnectionString("DefaultConnection");
            // var search = new Search(connectionString);
        }
    }
}
