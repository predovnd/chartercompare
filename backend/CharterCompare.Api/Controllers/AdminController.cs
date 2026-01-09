using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CharterCompare.Api.Data;
using CharterCompare.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CharterCompare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext dbContext, ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    private bool IsAdmin()
    {
        return User.FindFirst("IsAdmin")?.Value == "true";
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var totalOperators = await _dbContext.Providers.CountAsync(p => !p.IsAdmin);
        var totalRequests = await _dbContext.CharterRequests.CountAsync();
        var openRequests = await _dbContext.CharterRequests.CountAsync(r => r.Status == RequestStatus.Open);
        var totalQuotes = await _dbContext.Quotes.CountAsync();

        return Ok(new
        {
            totalOperators,
            totalRequests,
            openRequests,
            totalQuotes
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var providers = await _dbContext.Providers
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        
        var quoteCounts = await _dbContext.Quotes
            .GroupBy(q => q.ProviderId)
            .Select(g => new { ProviderId = g.Key, Count = g.Count() })
            .ToListAsync();
        
        var requestCounts = await _dbContext.Quotes
            .GroupBy(q => q.ProviderId)
            .Select(g => new { ProviderId = g.Key, Count = g.Select(q => q.CharterRequestId).Distinct().Count() })
            .ToListAsync();

        var users = providers.Select(p => new
        {
            id = p.Id,
            email = p.Email,
            name = p.Name,
            companyName = p.CompanyName,
            phone = p.Phone,
            externalProvider = p.ExternalProvider,
                isAdmin = p.IsAdmin,
            isActive = p.IsActive,
            createdAt = p.CreatedAt,
            lastLoginAt = p.LastLoginAt,
            quoteCount = quoteCounts.FirstOrDefault(q => q.ProviderId == p.Id)?.Count ?? 0,
            requestCount = requestCounts.FirstOrDefault(r => r.ProviderId == p.Id)?.Count ?? 0
        }).ToList();

        return Ok(users);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var requests = await _dbContext.CharterRequests
            .Include(r => r.Quotes)
            .ThenInclude(q => q.Provider)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                id = r.Id,
                sessionId = r.SessionId,
                requestData = r.RequestData,
                status = r.Status.ToString(),
                createdAt = r.CreatedAt,
                quoteCount = r.Quotes.Count,
                quotes = r.Quotes.Select(q => new
                {
                    id = q.Id,
                    providerName = q.Provider.Name,
                    providerEmail = q.Provider.Email,
                    price = q.Price,
                    currency = q.Currency,
                    notes = q.Notes,
                    status = q.Status.ToString(),
                    createdAt = q.CreatedAt
                })
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPost("users/{userId}/make-admin")]
    public async Task<IActionResult> MakeUserAdmin(int userId)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        // Check if an admin already exists
        var existingAdmin = await _dbContext.Providers
            .FirstOrDefaultAsync(p => p.IsAdmin && p.Id != userId);
        
        if (existingAdmin != null)
        {
            return BadRequest(new { error = "An admin already exists. Only one admin is allowed." });
        }

        var user = await _dbContext.Providers.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        user.IsAdmin = true;
        _dbContext.Providers.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} ({Email}) promoted to admin", userId, user.Email);

        return Ok(new
        {
            message = "User promoted to admin successfully",
            user = new
            {
                id = user.Id,
                email = user.Email,
                name = user.Name,
                isAdmin = user.IsAdmin
            }
        });
    }

    [HttpGet("users/by-email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var user = await _dbContext.Providers
            .FirstOrDefaultAsync(p => p.Email == email);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.Name,
            companyName = user.CompanyName,
            isAdmin = user.IsAdmin,
            isActive = user.IsActive,
            externalProvider = user.ExternalProvider,
            createdAt = user.CreatedAt
        });
    }
}
