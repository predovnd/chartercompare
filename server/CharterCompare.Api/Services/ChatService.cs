using CharterCompare.Api.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using CharterCompare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CharterCompare.Api.Services;

public class ChatService : IChatService
{
    private readonly IMemoryCache _cache;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChatService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private const string CacheKeyPrefix = "chat_session_";

    public ChatService(IMemoryCache cache, ApplicationDbContext dbContext, ILogger<ChatService> logger, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _cache = cache;
        _dbContext = dbContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public Task<StartChatResponse> StartChatAsync(StartChatRequest request)
    {
        var sessionId = GenerateSessionId();
        var replyText = "Hey — it's Alex from Charter Compare. I can help you find a charter bus. Where do you need to be picked up from?";

        var initialState = new ChatState
        {
            SessionId = sessionId,
            Step = ChatStep.Pickup,
            Data = new PartialCharterRequest(),
            IsComplete = false
        };

        _cache.Set($"{CacheKeyPrefix}{sessionId}", initialState, TimeSpan.FromHours(24));

        return Task.FromResult(new StartChatResponse
        {
            SessionId = sessionId,
            ReplyText = replyText,
            Icon = GetIconForStep(ChatStep.Pickup)
        });
    }

    public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request)
    {
        var cacheKey = $"{CacheKeyPrefix}{request.SessionId}";
        if (!_cache.TryGetValue(cacheKey, out ChatState? state) || state == null || state.SessionId != request.SessionId)
        {
            throw new InvalidOperationException("Invalid session");
        }

        var newState = new ChatState
        {
            SessionId = state.SessionId,
            Step = state.Step,
            Data = state.Data,
            IsComplete = state.IsComplete,
            WaitingForDateConfirmation = state.WaitingForDateConfirmation
        };

        var replyText = "";
        var isComplete = false;
        CharterRequest? finalPayload = null;
        string? icon = null;

        switch (state.Step)
        {
            case ChatStep.Pickup:
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.PickupLocation = new PartialLocationInfo
                {
                    RawInput = request.Text.Trim(),
                    ResolvedName = request.Text.Trim(),
                    Confidence = "low"
                };
                newState.Step = ChatStep.Destination;
                replyText = "And where are you going to?";
                icon = GetIconForStep(ChatStep.Destination);
                break;

            case ChatStep.Destination:
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.Destination = new PartialLocationInfo
                {
                    RawInput = request.Text.Trim(),
                    ResolvedName = request.Text.Trim(),
                    Confidence = "low"
                };
                
                // Check if user is authenticated - if not, ask for email
                var httpContextForEmail = _httpContextAccessor.HttpContext;
                bool isAuthenticated = false;
                if (httpContextForEmail != null)
                {
                    try
                    {
                        var authResult = await httpContextForEmail.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        if (authResult?.Succeeded == true && authResult.Principal != null)
                        {
                            httpContextForEmail.User = authResult.Principal;
                            isAuthenticated = httpContextForEmail.User.Identity?.IsAuthenticated == true;
                        }
                    }
                    catch (Exception authEx)
                    {
                        _logger.LogWarning(authEx, "Error during authentication check");
                    }
                }
                
                if (!isAuthenticated)
                {
                    // Not authenticated - ask for email
                    newState.Step = ChatStep.Email;
                    replyText = "Great! What's your email address? We'll send you quotes from available providers.";
                    icon = GetIconForStep(ChatStep.Email);
                }
                else
                {
                    // Authenticated - skip email and complete
                    newState.Step = ChatStep.Complete;
                    replyText = "Perfect! I've got your request. We'll send you quotes from available providers shortly.";
                    isComplete = true;
                    icon = GetIconForStep(ChatStep.Complete);
                }
                break;
                
            case ChatStep.Email:
                var emailInput = request.Text.Trim();
                if (!IsValidEmail(emailInput))
                {
                    replyText = "That doesn't look like a valid email address. Could you please provide your email?";
                    icon = GetIconForStep(ChatStep.Email);
                    break;
                }
                
                newState.Data.Customer ??= new PartialCustomerInfo();
                newState.Data.Customer.Email = emailInput;
                newState.Step = ChatStep.Complete;
                replyText = "Perfect! I've got your request. We'll send you quotes from available providers shortly.";
                isComplete = true;
                icon = GetIconForStep(ChatStep.Complete);

                // Build final payload
                finalPayload = BuildFinalPayload(newState);
                
                // Ensure email is set from chat data if provided
                if (finalPayload != null && !string.IsNullOrEmpty(newState.Data.Customer?.Email))
                {
                    finalPayload.Customer.Email = newState.Data.Customer.Email;
                }

                // Save to database with raw JSON
                if (finalPayload != null)
                {
                    try
                    {
                        var rawJson = System.Text.Json.JsonSerializer.Serialize(finalPayload, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        
                        // Get requester ID and email if user is authenticated
                        int? requesterId = null;
                        string? userEmail = null;
                        var httpContextForSave = _httpContextAccessor.HttpContext;
                        
                        // Explicitly authenticate the request to read the cookie (even for anonymous endpoints)
                        if (httpContextForSave != null)
                        {
                            try
                            {
                                var authResult = await httpContextForSave.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                if (authResult?.Succeeded == true && authResult.Principal != null)
                                {
                                    // Replace the user principal with the authenticated one
                                    httpContextForSave.User = authResult.Principal;
                                    _logger.LogInformation("Successfully authenticated user from cookie");
                                }
                                else
                                {
                                    _logger.LogInformation("Authentication attempt failed or no principal");
                                }
                            }
                            catch (Exception authEx)
                            {
                                _logger.LogWarning(authEx, "Error during explicit authentication");
                            }
                        }
                        
                        _logger.LogInformation("Checking authentication - IsAuthenticated: {IsAuthenticated}, HttpContext: {HasContext}", 
                            httpContextForSave?.User?.Identity?.IsAuthenticated ?? false, httpContextForSave != null);
                        
                        if (httpContextForSave?.User?.Identity?.IsAuthenticated == true)
                        {
                            // Log all claims for debugging
                            var allClaims = httpContextForSave.User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                            _logger.LogInformation("User claims: {Claims}", string.Join(", ", allClaims));
                            
                            // Try RequesterId claim first (for requesters)
                            var requesterIdClaim = httpContextForSave.User.FindFirst("RequesterId")?.Value;
                            if (requesterIdClaim != null && int.TryParse(requesterIdClaim, out var parsedId))
                            {
                                requesterId = parsedId;
                                _logger.LogInformation("Found RequesterId from RequesterId claim: {RequesterId}", requesterId);
                            }
                            
                            // Also try UserId claim and check if user is a requester by looking up the user
                            if (requesterId == null)
                            {
                                var userIdClaim = httpContextForSave.User.FindFirst("UserId")?.Value;
                                if (userIdClaim != null && int.TryParse(userIdClaim, out var parsedUserId))
                                {
                                    _logger.LogInformation("Found UserId claim: {UserId}, checking if requester...", parsedUserId);
                                    // Check if this user is a requester by looking up the user
                                    try
                                    {
                                        var user = await _dbContext.Users.FindAsync(new object[] { parsedUserId });
                                        if (user != null && user.Role == CharterCompare.Domain.Enums.UserRole.Requester)
                                        {
                                            requesterId = parsedUserId;
                                            _logger.LogInformation("Found RequesterId from UserId claim (user is requester): {RequesterId}", requesterId);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("UserId {UserId} is not a requester (Role: {Role})", parsedUserId, user?.Role);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Could not verify if user {UserId} is a requester", parsedUserId);
                                    }
                                }
                            }
                            
                            // Get email from claims
                            userEmail = httpContextForSave.User.FindFirst(ClaimTypes.Email)?.Value;
                            _logger.LogInformation("Authenticated user - Email: {Email}, RequesterId: {RequesterId}", userEmail, requesterId);
                        }
                        else
                        {
                            _logger.LogWarning("User is not authenticated when saving request. HttpContext available: {HasContext}", httpContextForSave != null);
                        }
                        
                        // Update finalPayload with authenticated user's email if available (but don't override email from chat)
                        if (userEmail != null && finalPayload != null && string.IsNullOrEmpty(finalPayload.Customer.Email))
                        {
                            finalPayload.Customer.Email = userEmail;
                            _logger.LogInformation("Updated finalPayload with authenticated user email: {Email}", userEmail);
                        }
                        
                        await SaveRequestAsync(newState.SessionId, finalPayload, rawJson, requesterId);
                        _logger.LogInformation("Request saved for session {SessionId}, RequesterId: {RequesterId}", newState.SessionId, requesterId);
                        
                        // Send notification email with quote link
                        try
                        {
                            var allRequests = await _dbContext.CharterRequests
                                .Where(r => r.SessionId == newState.SessionId)
                                .OrderByDescending(r => r.CreatedAt)
                                .FirstOrDefaultAsync();
                            
                            if (allRequests != null)
                            {
                                var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:5173";
                                var quoteLink = $"{frontendUrl}/quotes/{newState.SessionId}";
                                
                                var notificationService = _serviceProvider.GetRequiredService<CharterCompare.Application.Services.INotificationService>();
                                await notificationService.NotifyRequestSubmittedAsync(allRequests, quoteLink);
                            }
                        }
                        catch (Exception notifEx)
                        {
                            _logger.LogError(notifEx, "Failed to send request submission notification for session {SessionId}", newState.SessionId);
                            // Don't fail the request if notification fails
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save request for session {SessionId}", newState.SessionId);
                        // Don't fail the request if save fails, just log it
                    }
                }
                break;

            default:
                replyText = "I'm not sure how to help with that. Could you try rephrasing?";
                icon = "HelpCircle";
                break;
        }

        newState.IsComplete = isComplete;
        _cache.Set(cacheKey, newState, TimeSpan.FromHours(24));

        // Simulate latency
        await Task.Delay(Random.Shared.Next(300, 800));

        return new SendMessageResponse
        {
            ReplyText = replyText,
            IsComplete = isComplete,
            FinalPayload = finalPayload,
            Icon = icon
        };
    }

    private string GenerateSessionId()
    {
        return $"session_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }

    private string GetIconForStep(ChatStep step, bool isError = false)
    {
        if (isError) return "AlertCircle";

        return step switch
        {
            ChatStep.TripType => "Calendar",
            ChatStep.PassengerCount => "Users",
            ChatStep.Date => "CalendarDays",
            ChatStep.Pickup => "MapPin",
            ChatStep.Destination => "Navigation",
            ChatStep.TripFormat => "ArrowLeftRight",
            ChatStep.Timing => "Clock",
            ChatStep.Requirements => "ListChecks",
            ChatStep.Email => "Mail",
            ChatStep.Complete => "CheckCircle",
            _ => "MessageCircle"
        };
    }

    private int? ParsePassengerCount(string text)
    {
        var match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : null;
    }

    private bool IsMultiDayInput(string text)
    {
        var lower = text.ToLower();
        return Regex.IsMatch(lower, @"(\d+\s*-\s*\d+|\d+\s*to\s*\d+|\d+\s*thru\s*\d+)") ||
               Regex.IsMatch(lower, @"\d+\s*days?") ||
               Regex.IsMatch(lower, @"overnight|multi.?day|multiple\s*days?");
    }

    private (string ResolvedDate, string Confidence) ParseDate(string text)
    {
        var isoMatch = Regex.Match(text, @"(\d{4})-(\d{2})-(\d{2})");
        if (isoMatch.Success)
        {
            return (isoMatch.Value, "high");
        }

        var monthNames = new[] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
        var monthAbbr = new[] { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };

        var lower = text.ToLower();
        var hasMonth = monthNames.Any(m => lower.Contains(m)) || monthAbbr.Any(m => lower.Contains(m));
        var hasDay = Regex.IsMatch(text, @"\d{1,2}");

        if (hasMonth && hasDay)
        {
            return ("", "medium");
        }

        return ("", "low");
    }

    private string IsTripFormatClear(string text)
    {
        var lower = text.ToLower();
        if (Regex.IsMatch(lower, @"one.?way|single.?way|one.?direction"))
        {
            return "one_way";
        }
        if (Regex.IsMatch(lower, @"return|round.?trip|same.?day|back|returning"))
        {
            return "return_same_day";
        }
        return "unclear";
    }

    private List<string> ParseRequirements(string text)
    {
        var lower = text.Trim().ToLower();
        if (Regex.IsMatch(lower, @"^(none|no|n/a|na|nothing)$"))
        {
            return new List<string>();
        }
        return text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => s.Length > 0)
                   .ToList();
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
    }

    private CharterRequest BuildFinalPayload(ChatState state)
    {
        var trip = state.Data.Trip ?? new PartialTripInfo();

        // Get authenticated user's email if available
        string? userEmail = null;
        string? userName = null;
        string? userPhone = null;
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            userName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            userPhone = httpContext.User.FindFirst(ClaimTypes.MobilePhone)?.Value;
        }

        return new CharterRequest
        {
            Customer = new CustomerInfo
            {
                FirstName = userName?.Split(' ').FirstOrDefault() ?? "",
                LastName = userName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Phone = userPhone ?? "",
                Email = userEmail ?? ""
            },
            Trip = new TripInfo
            {
                Type = "",
                PassengerCount = 0,
                Date = new DateInfo
                {
                    RawInput = "",
                    ResolvedDate = "",
                    Confidence = "low"
                },
                PickupLocation = new LocationInfo
                {
                    RawInput = trip.PickupLocation?.RawInput ?? "",
                    ResolvedName = trip.PickupLocation?.ResolvedName ?? trip.PickupLocation?.RawInput ?? "",
                    Suburb = trip.PickupLocation?.Suburb ?? "",
                    State = trip.PickupLocation?.State ?? "",
                    Lat = trip.PickupLocation?.Lat,
                    Lng = trip.PickupLocation?.Lng,
                    Confidence = trip.PickupLocation?.Confidence ?? "low"
                },
                Destination = new LocationInfo
                {
                    RawInput = trip.Destination?.RawInput ?? "",
                    ResolvedName = trip.Destination?.ResolvedName ?? trip.Destination?.RawInput ?? "",
                    Suburb = trip.Destination?.Suburb ?? "",
                    State = trip.Destination?.State ?? "",
                    Lat = trip.Destination?.Lat,
                    Lng = trip.Destination?.Lng,
                    Confidence = trip.Destination?.Confidence ?? "low"
                },
                TripFormat = "one_way",
                Timing = new TimingInfo
                {
                    RawInput = "",
                    PickupTime = "",
                    ReturnTime = ""
                },
                SpecialRequirements = new List<string>()
            },
            Meta = new RequestMeta
            {
                Source = "webchat",
                CreatedAt = DateTime.UtcNow.ToString("O")
            }
        };
    }

    private async Task SaveRequestAsync(string sessionId, CharterRequest request, string rawJsonPayload, int? requesterId = null)
    {
        // Save to database
        // Convert from Api.Models.CharterRequest to Domain.Entities.CharterRequest
        var domainRequest = new CharterCompare.Domain.Entities.CharterRequest
        {
            Customer = new CharterCompare.Domain.Entities.CustomerInfo
            {
                FirstName = request.Customer.FirstName,
                LastName = request.Customer.LastName,
                Phone = request.Customer.Phone,
                Email = request.Customer.Email
            },
            Trip = new CharterCompare.Domain.Entities.TripInfo
            {
                Type = request.Trip.Type,
                PassengerCount = request.Trip.PassengerCount,
                Date = new CharterCompare.Domain.Entities.DateInfo
                {
                    RawInput = request.Trip.Date.RawInput,
                    ResolvedDate = request.Trip.Date.ResolvedDate,
                    Confidence = request.Trip.Date.Confidence
                },
                PickupLocation = new CharterCompare.Domain.Entities.LocationInfo
                {
                    RawInput = request.Trip.PickupLocation.RawInput,
                    ResolvedName = request.Trip.PickupLocation.ResolvedName,
                    Suburb = request.Trip.PickupLocation.Suburb,
                    State = request.Trip.PickupLocation.State,
                    Lat = request.Trip.PickupLocation.Lat,
                    Lng = request.Trip.PickupLocation.Lng,
                    Confidence = request.Trip.PickupLocation.Confidence
                },
                Destination = new CharterCompare.Domain.Entities.LocationInfo
                {
                    RawInput = request.Trip.Destination.RawInput,
                    ResolvedName = request.Trip.Destination.ResolvedName,
                    Suburb = request.Trip.Destination.Suburb,
                    State = request.Trip.Destination.State,
                    Lat = request.Trip.Destination.Lat,
                    Lng = request.Trip.Destination.Lng,
                    Confidence = request.Trip.Destination.Confidence
                },
                TripFormat = request.Trip.TripFormat,
                Timing = new CharterCompare.Domain.Entities.TimingInfo
                {
                    RawInput = request.Trip.Timing.RawInput,
                    PickupTime = request.Trip.Timing.PickupTime,
                    ReturnTime = request.Trip.Timing.ReturnTime
                },
                SpecialRequirements = request.Trip.SpecialRequirements
            },
            Meta = new CharterCompare.Domain.Entities.RequestMeta
            {
                Source = request.Meta.Source,
                CreatedAt = request.Meta.CreatedAt
            }
        };

        var requestRecord = new CharterCompare.Domain.Entities.CharterRequestRecord
        {
            SessionId = sessionId,
            RequestData = domainRequest,
            RawJsonPayload = rawJsonPayload,
            RequesterId = requesterId,
            Email = request.Customer.Email, // Capture email for easy access and notifications
            CreatedAt = DateTime.UtcNow,
            Status = CharterCompare.Domain.Enums.RequestStatus.Draft // New requests start as Draft, need admin review
        };
        
        _logger.LogInformation("Creating request record - SessionId: {SessionId}, RequesterId: {RequesterId}, Email: {Email}", 
            sessionId, requesterId, request.Customer.Email);
        _dbContext.CharterRequests.Add(requestRecord);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Request saved successfully - Id: {RequestId}, SessionId: {SessionId}, RequesterId: {RequesterId}", 
            requestRecord.Id, sessionId, requesterId);
    }

    private async Task WriteJsonToFileAsync(CharterRequest request)
    {
        // Create requests directory if it doesn't exist
        var requestsDir = Path.Combine(Directory.GetCurrentDirectory(), "requests");
        if (!Directory.Exists(requestsDir))
        {
            Directory.CreateDirectory(requestsDir);
        }

        // Generate filename with timestamp and email (sanitized)
        var emailSanitized = string.IsNullOrEmpty(request.Customer.Email) 
            ? "unknown" 
            : request.Customer.Email.Replace("@", "_at_").Replace(".", "_");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var filename = $"charter-request_{timestamp}_{emailSanitized}.json";
        var filePath = Path.Combine(requestsDir, filename);

        // Serialize and write to file
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = System.Text.Json.JsonSerializer.Serialize(request, jsonOptions);
        await File.WriteAllTextAsync(filePath, json, System.Text.Encoding.UTF8);

        _logger.LogInformation("JSON payload written to {FilePath}", filePath);
    }
}

// Internal state models
internal class ChatState
{
    public string SessionId { get; set; } = string.Empty;
    public ChatStep Step { get; set; }
    public PartialCharterRequest Data { get; set; } = new();
    public bool IsComplete { get; set; }
    public bool? WaitingForDateConfirmation { get; set; }
}

internal enum ChatStep
{
    TripType,
    PassengerCount,
    Date,
    Pickup,
    Destination,
    TripFormat,
    Timing,
    Requirements,
    Email,
    Complete
}

internal class PartialCharterRequest
{
    public PartialCustomerInfo? Customer { get; set; }
    public PartialTripInfo? Trip { get; set; }
}

internal class PartialCustomerInfo
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

internal class PartialTripInfo
{
    public string? Type { get; set; }
    public int? PassengerCount { get; set; }
    public PartialDateInfo? Date { get; set; }
    public PartialLocationInfo? PickupLocation { get; set; }
    public PartialLocationInfo? Destination { get; set; }
    public string? TripFormat { get; set; }
    public PartialTimingInfo? Timing { get; set; }
    public List<string>? SpecialRequirements { get; set; }
}

internal class PartialDateInfo
{
    public string RawInput { get; set; } = string.Empty;
    public string ResolvedDate { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
}

internal class PartialLocationInfo
{
    public string RawInput { get; set; } = string.Empty;
    public string ResolvedName { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string Confidence { get; set; } = string.Empty;
}

internal class PartialTimingInfo
{
    public string RawInput { get; set; } = string.Empty;
    public string PickupTime { get; set; } = string.Empty;
    public string ReturnTime { get; set; } = string.Empty;
}
