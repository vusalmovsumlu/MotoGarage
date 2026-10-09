using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController, Route("api/business-applications"), Authorize]
public class BusinessApplicationController(IBusinessApplicationService service) : ControllerBase
{
    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await service.GetMineAsync(userId));
    }
}
