using BookShelf.Lib;
using BookShelf.Lib.Model;
using BookShelf.Lib.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookShelf.Pages;

public class IndexModel : PageModel
{
    private readonly GCSService listService;

    public GCSOptions Options { get; }

    public IndexModel(GCSOptions options, GCSService listService)
    {
        Options = options;
        this.listService = listService;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public List<GCSObjectModel> List(string bucket)
    {
        return listService.FileList(bucket);
    }

}
