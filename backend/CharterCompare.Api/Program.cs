using CharterCompare.Application.MediatR;
using CharterCompare.Application.Storage;
using CharterCompare.Infrastructure.Data;
using CharterCompare.Infrastructure.Storage;
using CharterCompare.Domain.Entities;
using CharterCompare.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using UserAttributeType = CharterCompare.Domain.Enums.UserAttributeType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// Add database - use Infrastructure DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=CharterCompare;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register Storage (Infrastructure)
builder.Services.AddScoped<IStorage, SqlStorage>();

// Register Database Migrator (Infrastructure)
builder.Services.AddScoped<CharterCompare.Infrastructure.Migrations.IDatabaseMigrator, CharterCompare.Infrastructure.Migrations.SqlServerDatabaseMigrator>();

// Register Geocoding Service
builder.Services.AddHttpClient<CharterCompare.Application.Services.NominatimGeocodingService>();
builder.Services.AddScoped<CharterCompare.Application.Services.IGeocodingService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(CharterCompare.Application.Services.NominatimGeocodingService));
    var logger = sp.GetRequiredService<ILogger<CharterCompare.Application.Services.NominatimGeocodingService>>();
    return new CharterCompare.Application.Services.NominatimGeocodingService(httpClient, logger);
});

// Register MediatR
builder.Services.AddScoped<IMediator, SimpleMediator>();

// Register all handlers as their interface types
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Auth.OperatorLoginCommand, CharterCompare.Application.Requests.Auth.OperatorLoginResponse>, CharterCompare.Application.Handlers.Auth.OperatorLoginHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Auth.OperatorRegisterCommand, CharterCompare.Application.Requests.Auth.OperatorRegisterResponse>, CharterCompare.Application.Handlers.Auth.OperatorRegisterHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Auth.RequesterLoginCommand, CharterCompare.Application.Requests.Auth.RequesterLoginResponse>, CharterCompare.Application.Handlers.Auth.RequesterLoginHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Auth.RequesterRegisterCommand, CharterCompare.Application.Requests.Auth.RequesterRegisterResponse>, CharterCompare.Application.Handlers.Auth.RequesterRegisterHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Auth.GetCurrentUserQuery, CharterCompare.Application.Requests.Auth.GetCurrentUserResponse>, CharterCompare.Application.Handlers.Auth.GetCurrentUserHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Auth.CreateAdminCommand, CharterCompare.Application.Requests.Auth.CreateAdminResponse>, CharterCompare.Application.Handlers.Auth.CreateAdminHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Provider.GetProviderRequestsQuery, CharterCompare.Application.Requests.Provider.GetProviderRequestsResponse>, CharterCompare.Application.Handlers.Provider.GetProviderRequestsHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Provider.GetProviderQuotesQuery, CharterCompare.Application.Requests.Provider.GetProviderQuotesResponse>, CharterCompare.Application.Handlers.Provider.GetProviderQuotesHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Provider.SubmitQuoteCommand, CharterCompare.Application.Requests.Provider.SubmitQuoteResponse>, CharterCompare.Application.Handlers.Provider.SubmitQuoteHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Requester.GetRequesterRequestsQuery, CharterCompare.Application.Requests.Requester.GetRequesterRequestsResponse>, CharterCompare.Application.Handlers.Requester.GetRequesterRequestsHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.GetAdminStatsQuery, CharterCompare.Application.Requests.Admin.GetAdminStatsResponse>, CharterCompare.Application.Handlers.Admin.GetAdminStatsHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.GetAdminUsersQuery, CharterCompare.Application.Requests.Admin.GetAdminUsersResponse>, CharterCompare.Application.Handlers.Admin.GetAdminUsersHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.GetAdminRequestsQuery, CharterCompare.Application.Requests.Admin.GetAdminRequestsResponse>, CharterCompare.Application.Handlers.Admin.GetAdminRequestsHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.UpdateUserAttributesCommand, CharterCompare.Application.Requests.Admin.UpdateUserAttributesResponse>, CharterCompare.Application.Handlers.Admin.UpdateUserAttributesHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.ConfigureOperatorCoverageCommand, CharterCompare.Application.Requests.Admin.ConfigureOperatorCoverageResponse>, CharterCompare.Application.Handlers.Admin.ConfigureOperatorCoverageHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.GetOperatorCoveragesQuery, CharterCompare.Application.Requests.Admin.GetOperatorCoveragesResponse>, CharterCompare.Application.Handlers.Admin.GetOperatorCoveragesHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.UpdateOperatorCoverageCommand, CharterCompare.Application.Requests.Admin.UpdateOperatorCoverageResponse>, CharterCompare.Application.Handlers.Admin.UpdateOperatorCoverageHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.DeleteOperatorCoverageCommand, CharterCompare.Application.Requests.Admin.DeleteOperatorCoverageResponse>, CharterCompare.Application.Handlers.Admin.DeleteOperatorCoverageHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.UpdateRequestLocationCommand, CharterCompare.Application.Requests.Admin.UpdateRequestLocationResponse>, CharterCompare.Application.Handlers.Admin.UpdateRequestLocationHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.PublishRequestCommand, CharterCompare.Application.Requests.Admin.PublishRequestResponse>, CharterCompare.Application.Handlers.Admin.PublishRequestHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.WithdrawRequestCommand, CharterCompare.Application.Requests.Admin.WithdrawRequestResponse>, CharterCompare.Application.Handlers.Admin.WithdrawRequestHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.UpdateUserActiveStatusCommand, CharterCompare.Application.Requests.Admin.UpdateUserActiveStatusResponse>, CharterCompare.Application.Handlers.Admin.UpdateUserActiveStatusHandler>();

// Register legacy services (ChatService still uses old models)
builder.Services.AddScoped<CharterCompare.Api.Services.IChatService, CharterCompare.Api.Services.ChatService>();
builder.Services.AddMemoryCache(); // For chat session storage

// Add CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://localhost:5174",
                "http://localhost:5175",
                "http://localhost:3000",
                "http://127.0.0.1:5173", 
                "http://127.0.0.1:5174",
                "http://127.0.0.1:5175",
                "http://127.0.0.1:3000",
                "https://localhost:5173",
                "https://localhost:5174",
                "https://localhost:5175"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
    {
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    }
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".AspNetCore.CharterCompare.Cookies";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Only add Google authentication if credentials are provided
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/auth/google-callback";
        options.SaveTokens = true;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.Name = ".AspNetCore.CharterCompare.Correlation.Google";
        options.CorrelationCookie.Path = "/";
        
        var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";
        options.Events.OnCreatingTicket = async context =>
        {
            var claims = context.Principal?.Claims.ToList();
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var externalId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(externalId))
            {
                var storage = context.HttpContext.RequestServices.GetRequiredService<IStorage>();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                var isRequester = context.Properties.Items.TryGetValue("userType", out var userTypeValue) && userTypeValue == "requester";
                
                var user = await storage.GetUserByExternalIdAsync(externalId, "Google");
                
                if (isRequester)
                {
                    if (user == null)
                    {
                        user = new User
                        {
                            Email = email,
                            Name = name ?? email,
                            ExternalId = externalId,
                            ExternalProvider = "Google",
                            Role = UserRole.Requester,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        await storage.CreateUserAsync(user);
                        // Set default attribute: Individual for requesters
                        await storage.AddUserAttributeAsync(user.Id, UserAttributeType.Individual);
                        logger.LogInformation("Created new requester: {UserId}", user.Id);
                    }
                    else
                    {
                        user.LastLoginAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(name) && user.Name != name)
                        {
                            user.Name = name;
                        }
                        await storage.UpdateUserAsync(user);
                        await storage.SaveChangesAsync();
                        logger.LogInformation("Updated existing requester: {UserId}", user.Id);
                    }

                    var sessionClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim("UserId", user.Id.ToString()),
                        new Claim("RequesterId", user.Id.ToString()) // Legacy claim for backward compatibility
                    };

                    var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Principal = new ClaimsPrincipal(claimsIdentity);
                    context.Properties.RedirectUri = $"{frontendUrl}/requester/dashboard";
                }
                else
                {
                    if (user == null)
                    {
                        user = new User
                        {
                            Email = email,
                            Name = name ?? email,
                            ExternalId = externalId,
                            ExternalProvider = "Google",
                            Role = UserRole.Operator,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        await storage.CreateUserAsync(user);
                        // Set default attribute: Bus for operators
                        await storage.AddUserAttributeAsync(user.Id, UserAttributeType.Bus);
                        logger.LogInformation("Created new operator: {UserId}", user.Id);
                    }
                    else
                    {
                        user.LastLoginAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(name) && user.Name != name)
                        {
                            user.Name = name;
                        }
                        await storage.UpdateUserAsync(user);
                        await storage.SaveChangesAsync();
                        logger.LogInformation("Updated existing operator: {UserId}", user.Id);
                    }

                    var sessionClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim("UserId", user.Id.ToString()),
                        new Claim("ProviderId", user.Id.ToString()), // Legacy claim for backward compatibility
                        new Claim("IsAdmin", user.IsAdmin.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Principal = new ClaimsPrincipal(claimsIdentity);
                    context.Properties.RedirectUri = $"{frontendUrl}/provider/dashboard";
                }
            }
            
            await Task.CompletedTask;
        };
    });
}

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<CharterCompare.Infrastructure.Migrations.IDatabaseMigrator>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await migrator.MigrateAsync();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database migration.");
        // Don't delete and recreate in production - just log the error
        if (app.Environment.IsDevelopment())
        {
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                logger.LogWarning("Development mode: Attempting to recreate database...");
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Database recreated successfully.");
            }
            catch (Exception recreateEx)
            {
                logger.LogError(recreateEx, "Failed to recreate database.");
            }
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLocalhost");

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var origin = context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            Console.WriteLine($"[CORS] Request from origin: {origin}");
        }
        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
