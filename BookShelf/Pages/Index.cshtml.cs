using BookShelf.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookShelf.Pages;

public class IndexModel : PageModel
{
    private readonly BookService _bookService;

    public Options Options { get; }

    public IndexModel(Options options, BookService listService)
    {
        Options = options;
        _bookService = listService;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public List<BookService.ObjectEntry> List(string bucket)
    {
        return _bookService.GetObjects(bucket);
    }

}
