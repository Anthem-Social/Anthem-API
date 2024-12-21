using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("token")]
public class TokenController
(
    AuthorizationService authorizationService,
    TokenService tokenService
) : ControllerBase
{
    private readonly AuthorizationService _authorizationService = authorizationService;
    private readonly TokenService _tokenService = tokenService;

    [HttpPost("swap")]
    public async Task<IActionResult> Swap([FromForm] string code)
    {
        var swap = await _tokenService.Swap(code);
        if (swap.Data is null || swap.IsFailure) return StatusCode(500);

        JsonElement json = JsonDocument.Parse(swap.Data!).RootElement;

        var save = await _authorizationService.Save(json);
        if (save.Data is null || save.IsFailure) return StatusCode(500);

        return Ok(swap.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromForm] string refreshToken)
    {
        var refresh = await _tokenService.Refresh(refreshToken);
        if (refresh.Data is null || refresh.IsFailure) return StatusCode(500);

        JsonElement json = JsonDocument.Parse(refresh.Data!).RootElement;

        var save = await _authorizationService.Save(json);
        if (save.Data is null || save.IsFailure) return StatusCode(500);

        return Ok(refresh.Data);
    }
}
