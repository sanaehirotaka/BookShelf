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

    [HttpGet("{id}")]
    [Produces("application/json")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any, NoStore = false)]

    public BookService.BookEntry? List(string id)
    {
        return _bookService.GetEntry(id);
    }

    [HttpGet("{id}/{page}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task Page(string id, string page)
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
        Response.ContentLength = _bookService.GetEntry(id)!.Files.Single(f => f.Name == page).Size;
        await _bookService.GetPage(id, page).CopyToAsync(Response.Body);
    }

    [HttpGet("{id}")]
    [Produces("image/png")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task Thumb(string id)
    {
        var stream = _bookService.GetThumnail(id);
        Response.ContentLength = stream.Length;
        await stream.CopyToAsync(Response.Body);
    }
}
