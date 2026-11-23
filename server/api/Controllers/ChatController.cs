using api.Configuration;
using api.DTOs.Chat;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

[ApiController]
[Route("api/chat")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Chat)]
public sealed class ChatController(ChatService chatService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(12 * 1024)]
    public async Task<IActionResult> Reply([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request.History.Any(turn => turn.Role is not ("user" or "assistant")))
            return BadRequest(new { message = "Invalid conversation history." });
        var result = await chatService.GetReplyAsync(request, cancellationToken);
        return result is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Chat is temporarily unavailable." })
            : Ok(new ChatResponse(result));
    }
}
