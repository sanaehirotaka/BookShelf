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
        bool isMobile = false;
        if (Request.Headers.ContainsKey("sec-ch-ua-mobile"))
        {
            // sec-ch-ua-mobile ヘッダーが存在する場合
            string secChUaMobile = Request.Headers["sec-ch-ua-mobile"].ToString();
            isMobile = secChUaMobile == "?1";
        }
        var entry = _bookService.GetPage(id, page, isMobile);
        if (entry == null)
        {
            return;
        }
        Response.ContentType = entry.MimeType;
        Response.ContentLength = entry.Size;
        await entry.Stream.CopyToAsync(Response.Body);
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


    [HttpPost]
    [Produces("application/json")]
    public async Task<DeleteResponse> Delete([FromBody] DeleteRequest body)
    {
        _bookService.Delete(body.Id);
        return new(true);
    }

    public record DeleteRequest(string Id);

    public record DeleteResponse(bool Sucsess);

    [HttpPost]
    [Produces("application/json")]
    public async Task<MoveResponse> Move([FromBody] MoveRequest body)
    {
        _bookService.Move(body.Id, body.After);
        return new(true);
    }

    public record MoveRequest(string Id, string After);

    public record MoveResponse(bool Sucsess);

}
