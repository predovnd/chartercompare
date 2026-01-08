using CharterCompare.Api.Models;

namespace CharterCompare.Api.Services;

public interface IChatService
{
    Task<StartChatResponse> StartChatAsync(StartChatRequest request);
    Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request);
}
