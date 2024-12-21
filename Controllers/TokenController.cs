using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("token")]
public class TokenController
(
    AuthorizationService authorizationService
) : ControllerBase
{
    private readonly AuthorizationService _authorizationService = authorizationService;

    [HttpPost("swap")]
    public async Task<IActionResult> Swap([FromForm] string code)
    {
        var swapResult = await _authorizationService.Swap(code);
        if (swapResult.Data is null || swapResult.IsFailure) return BadRequest();

        JsonElement json = JsonDocument.Parse(swapResult.Data!).RootElement;

        var saveResult = await _authorizationService.Save(json);
        if (saveResult.Data is null || saveResult.IsFailure) return BadRequest();

        return Ok(swapResult.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromForm] string refreshToken)
    {
        var refreshResult = await _authorizationService.Refresh(refreshToken);
        if (refreshResult.Data is null || refreshResult.IsFailure) return BadRequest();

        JsonElement json = JsonDocument.Parse(refreshResult.Data!).RootElement;

        var saveResult = await _authorizationService.Save(json);
        if (saveResult.Data is null || saveResult.IsFailure) return BadRequest();

        return Ok(refreshResult.Data);
    }
}
