using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.API.Controllers;

[ApiController, Route("api/chat/conversations/{conversationId:int:min(1)}/attachments")]
[Authorize(Roles = "User,Mechanic,Store")]
public class ChatAttachmentController(IChatAttachmentService attachments) : ControllerBase
{
    int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost, Consumes("multipart/form-data"), EnableRateLimiting("image-upload")]
    [RequestSizeLimit(27 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 27 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int conversationId, IFormFile file, CancellationToken token)
    {
        if (file == null || file.Length == 0 || file.Length > 25 * 1024 * 1024)
            return BadRequest(new { message = "Şəkil və ya video seçin. Fayl maksimum 25 MB ola bilər." });
        await using var stream = file.OpenReadStream();
        return Ok(await attachments.UploadAsync(conversationId, UserId, stream, file.FileName, file.ContentType, token));
    }
    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Read(int conversationId, Guid id, CancellationToken token)
    {
        var result = await attachments.ReadAsync(id, conversationId, UserId, token);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(result.Bytes, result.ContentType);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(int conversationId, Guid id)
    { await attachments.DeletePendingAsync(id, conversationId, UserId); return NoContent(); }
}
