using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookShelf.Pages
{
    public class BookModel : PageModel
    {
        public string Id { get; set; } = default!;

        public void OnGet(string id)
        {
            Id = id;
        }
    }
}
