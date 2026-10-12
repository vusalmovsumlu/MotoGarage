using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.API.Controllers;

[ApiController, Route("api/business/statistics"), Authorize(Roles = "Mechanic,Store")]
public class BusinessStatisticsController(IBusinessStatisticsService statistics) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(DateOnly from, DateOnly to) =>
        Ok(await statistics.GetAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), from, to));
}
