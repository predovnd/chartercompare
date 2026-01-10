using Microsoft.AspNetCore.Mvc;
using CharterCompare.Api.Models;
using CharterCompare.Api.Services;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<ActionResult<StartChatResponse>> StartChat([FromBody] StartChatRequest request)
    {
        try
        {
            var response = await _chatService.StartChatAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting chat");
            return StatusCode(500, new { error = "Failed to start chat" });
        }
    }

    [HttpPost("message")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            var response = await _chatService.SendMessageAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid session: {SessionId}", request.SessionId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { error = "Failed to send message" });
        }
    }
}
