using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/images")]
[Authorize]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;

    public ImageController(
        IImageService imageService)
    {
        _imageService = imageService;
    }

    // POST: api/images/upload
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file)
    {
        if (file is null ||
            file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Şəkil seçilməyib."
            });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new
            {
                message = "Şəklin ölçüsü maksimum 5 MB ola bilər."
            });
        }

        await using var stream =
            file.OpenReadStream();

        var result =
            await _imageService.UploadAsync(
                stream,
                file.FileName);

        return Ok(result);
    }
}