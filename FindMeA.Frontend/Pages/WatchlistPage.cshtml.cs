using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Find_Me_A; // backend namespace
using System.Collections.Generic;

namespace FindMeA.Frontend.Pages
{
    public class WatchlistPageModel : PageModel
    {
        private readonly IConfiguration _config;

        public List<WatchList.WatchedItem> Items { get; set; }

        public WatchlistPageModel(IConfiguration config)
        {
            _config = config;
        }

        public void OnGet()
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");

            var watchList = new WatchList(connectionString);

            // Call the correct backend method
            Items = watchList.GetWatchedList("Gabriela");
        }
    }
}
