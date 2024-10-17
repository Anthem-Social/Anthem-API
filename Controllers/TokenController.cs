using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnthemAPI.Controllers;

[Route("token")]
[ApiController]
public class TokenController
(
    TokenService tokenService
) : ControllerBase
{
    private readonly TokenService _tokenService = tokenService;

    [HttpPost("swap")]
    public async Task<IActionResult> Swap([FromForm] string code)
    {
        var result = await _tokenService.Swap(code);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return StatusCode(500);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromForm] string refresh_token)
    {
        var result = await _tokenService.Refresh(refresh_token);

        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return StatusCode(500);
    }
}