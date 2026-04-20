using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FindMeA.Frontend.Pages
{
    public class LogInPageModel : PageModel
    {
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // Temporary placeholder logic
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return Page();
            }

            // TODO: Replace with real authentication later
            if (Username == "test" && Password == "123")
            {
                return RedirectToPage("/HomePage");
            }

            ErrorMessage = "Invalid username or password.";
            return Page();
        }
    }
}
