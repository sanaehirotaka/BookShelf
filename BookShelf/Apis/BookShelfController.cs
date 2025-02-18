using BookShelf.Lib;
using Microsoft.AspNetCore.Mvc;

namespace BookShelf.Apis;

[ApiController]
[Route("api/[controller]/[action]")]
public class BookShelfController : ControllerBase
{
    private readonly BookService _bookService;

    public BookShelfController(BookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    [Produces("application/json")]
    public BookService.BookEntry List(string id)
    {
        return _bookService.GetEntry(id);
    }

    [HttpGet]
    public void Page(string id, string page)
    {
        if (page.EndsWith(".webp"))
        {
            Response.ContentType = "image/webp";
        }
        else if (page.EndsWith(".jpg"))
        {
            Response.ContentType = "image/jpeg";
        }
        else if (page.EndsWith(".png"))
        {
            Response.ContentType = "image/png";
        }
        Response.ContentLength = _bookService.GetEntry(id).Files.Single(f => f.Name == page).Size;
        _bookService.GetPage(id, page).CopyTo(Response.Body);
    }
}
