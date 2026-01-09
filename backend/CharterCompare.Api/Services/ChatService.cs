using CharterCompare.Api.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using CharterCompare.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CharterCompare.Api.Services;

public class ChatService : IChatService
{
    private readonly IMemoryCache _cache;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChatService> _logger;
    private const string CacheKeyPrefix = "chat_session_";

    public ChatService(IMemoryCache cache, ApplicationDbContext dbContext, ILogger<ChatService> logger)
    {
        _cache = cache;
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<StartChatResponse> StartChatAsync(StartChatRequest request)
    {
        var sessionId = GenerateSessionId();
        var replyText = "Hey — it's Alex from Charter Compare. I can help you sort a charter bus. What's the trip for — for example a school trip, corporate event, wedding, sports team, or something else?";

        var initialState = new ChatState
        {
            SessionId = sessionId,
            Step = ChatStep.TripType,
            Data = new PartialCharterRequest(),
            IsComplete = false
        };

        _cache.Set($"{CacheKeyPrefix}{sessionId}", initialState, TimeSpan.FromHours(24));

        return Task.FromResult(new StartChatResponse
        {
            SessionId = sessionId,
            ReplyText = replyText,
            Icon = GetIconForStep(ChatStep.TripType)
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
            case ChatStep.TripType:
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.Type = request.Text.Trim();
                newState.Step = ChatStep.PassengerCount;
                replyText = "About how many passengers will be travelling?";
                icon = GetIconForStep(ChatStep.PassengerCount);
                break;

            case ChatStep.PassengerCount:
                var count = ParsePassengerCount(request.Text);
                if (count == null || count <= 0)
                {
                    replyText = "I need a number — about how many passengers will be travelling?";
                    icon = GetIconForStep(ChatStep.PassengerCount, true);
                    break;
                }
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.PassengerCount = count.Value;
                newState.Step = ChatStep.Date;
                replyText = "What date is the trip? We're just booking single-day trips at the moment.";
                icon = GetIconForStep(ChatStep.Date);
                break;

            case ChatStep.Date:
                if (state.WaitingForDateConfirmation == true)
                {
                    var lower = request.Text.ToLower();
                    if (lower.Contains("yes") || lower.Contains("yeah") || lower.Contains("yep") || lower.Contains("sure") || lower.Contains("ok"))
                    {
                        newState.WaitingForDateConfirmation = false;
                        replyText = "What date is the trip?";
                        icon = GetIconForStep(ChatStep.Date);
                        break;
                    }
                    else
                    {
                        replyText = "No worries — we'll keep that in mind for future. For now, what date would work for a single-day trip?";
                        newState.WaitingForDateConfirmation = false;
                        icon = GetIconForStep(ChatStep.Date);
                        break;
                    }
                }
                if (IsMultiDayInput(request.Text))
                {
                    replyText = "At the moment we can only help with single-day trips — would it still work as a one-day booking?";
                    newState.WaitingForDateConfirmation = true;
                    icon = GetIconForStep(ChatStep.Date);
                    break;
                }
                var dateParse = ParseDate(request.Text);
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.Date = new PartialDateInfo
                {
                    RawInput = request.Text.Trim(),
                    ResolvedDate = dateParse.ResolvedDate,
                    Confidence = dateParse.Confidence
                };
                newState.Step = ChatStep.Pickup;
                replyText = "Where will everyone be picked up from? A suburb, landmark, or postcode is fine.";
                icon = GetIconForStep(ChatStep.Pickup);
                break;

            case ChatStep.Pickup:
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.PickupLocation = new PartialLocationInfo
                {
                    RawInput = request.Text.Trim(),
                    Confidence = "low"
                };
                newState.Step = ChatStep.Destination;
                replyText = "And where's the main destination or drop-off?";
                icon = GetIconForStep(ChatStep.Destination);
                break;

            case ChatStep.Destination:
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.Destination = new PartialLocationInfo
                {
                    RawInput = request.Text.Trim(),
                    Confidence = "low"
                };
                newState.Step = ChatStep.TripFormat;
                replyText = "Is it a one-way trip or a return on the same day?";
                icon = GetIconForStep(ChatStep.TripFormat);
                break;

            case ChatStep.TripFormat:
                var format = IsTripFormatClear(request.Text);
                if (format == "unclear")
                {
                    replyText = "Just to confirm — one-way, or return on the same day?";
                    icon = GetIconForStep(ChatStep.TripFormat, true);
                    break;
                }
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.TripFormat = format;
                newState.Step = ChatStep.Timing;
                replyText = "Do you have rough pickup and return times?";
                icon = GetIconForStep(ChatStep.Timing);
                break;

            case ChatStep.Timing:
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.Timing = new PartialTimingInfo
                {
                    RawInput = request.Text.Trim()
                };
                newState.Step = ChatStep.Requirements;
                replyText = "Any special requirements? For example luggage space, wheelchair access, or onboard features.";
                icon = GetIconForStep(ChatStep.Requirements);
                break;

            case ChatStep.Requirements:
                var requirements = ParseRequirements(request.Text);
                newState.Data.Trip ??= new PartialTripInfo();
                newState.Data.Trip.SpecialRequirements = requirements;
                newState.Step = ChatStep.Email;
                replyText = "What's the best email address to send the comparison results to?";
                icon = GetIconForStep(ChatStep.Email);
                break;

            case ChatStep.Email:
                if (!IsValidEmail(request.Text.Trim()))
                {
                    replyText = "That doesn't look like a valid email — what's the best email to send the results to?";
                    icon = GetIconForStep(ChatStep.Email, true);
                    break;
                }
                newState.Data.Customer ??= new PartialCustomerInfo();
                newState.Data.Customer.Email = request.Text.Trim();
                newState.Step = ChatStep.Complete;
                replyText = "Great — I've got everything I need. I'll pass this through now and someone will be in touch shortly with the best available options.";
                isComplete = true;
                icon = GetIconForStep(ChatStep.Complete);

                // Build final payload
                finalPayload = BuildFinalPayload(newState);

                // Save to database and file
                if (finalPayload != null)
                {
                    try
                    {
                        await SaveRequestAsync(newState.SessionId, finalPayload);
                        await WriteJsonToFileAsync(finalPayload);
                        _logger.LogInformation("Request saved for session {SessionId}", newState.SessionId);
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
        var customer = state.Data.Customer ?? new PartialCustomerInfo();

        return new CharterRequest
        {
            Customer = new CustomerInfo
            {
                FirstName = customer.FirstName ?? "",
                LastName = customer.LastName ?? "",
                Phone = customer.Phone ?? "",
                Email = customer.Email ?? ""
            },
            Trip = new TripInfo
            {
                Type = trip.Type ?? "",
                PassengerCount = trip.PassengerCount ?? 0,
                Date = new DateInfo
                {
                    RawInput = trip.Date?.RawInput ?? "",
                    ResolvedDate = trip.Date?.ResolvedDate ?? "",
                    Confidence = trip.Date?.Confidence ?? "low"
                },
                PickupLocation = new LocationInfo
                {
                    RawInput = trip.PickupLocation?.RawInput ?? "",
                    ResolvedName = trip.PickupLocation?.ResolvedName ?? "",
                    Suburb = trip.PickupLocation?.Suburb ?? "",
                    State = trip.PickupLocation?.State ?? "",
                    Lat = trip.PickupLocation?.Lat,
                    Lng = trip.PickupLocation?.Lng,
                    Confidence = trip.PickupLocation?.Confidence ?? "low"
                },
                Destination = new LocationInfo
                {
                    RawInput = trip.Destination?.RawInput ?? "",
                    ResolvedName = trip.Destination?.ResolvedName ?? "",
                    Suburb = trip.Destination?.Suburb ?? "",
                    State = trip.Destination?.State ?? "",
                    Lat = trip.Destination?.Lat,
                    Lng = trip.Destination?.Lng,
                    Confidence = trip.Destination?.Confidence ?? "low"
                },
                TripFormat = trip.TripFormat ?? "one_way",
                Timing = new TimingInfo
                {
                    RawInput = trip.Timing?.RawInput ?? "",
                    PickupTime = trip.Timing?.PickupTime ?? "",
                    ReturnTime = trip.Timing?.ReturnTime ?? ""
                },
                SpecialRequirements = trip.SpecialRequirements ?? new List<string>()
            },
            Meta = new RequestMeta
            {
                Source = "webchat",
                CreatedAt = DateTime.UtcNow.ToString("O")
            }
        };
    }

    private async Task SaveRequestAsync(string sessionId, CharterRequest request)
    {
        // Save to database
        var requestRecord = new CharterRequestRecord
        {
            SessionId = sessionId,
            RequestData = request,
            CreatedAt = DateTime.UtcNow,
            Status = RequestStatus.Open
        };
        _dbContext.CharterRequests.Add(requestRecord);
        await _dbContext.SaveChangesAsync();
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
