using CharterCompare.Application.MediatR;
using CharterCompare.Application.Storage;
using CharterCompare.Infrastructure.Data;
using CharterCompare.Infrastructure.Storage;
using CharterCompare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

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
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Provider.SubmitQuoteCommand, CharterCompare.Application.Requests.Provider.SubmitQuoteResponse>, CharterCompare.Application.Handlers.Provider.SubmitQuoteHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Requester.GetRequesterRequestsQuery, CharterCompare.Application.Requests.Requester.GetRequesterRequestsResponse>, CharterCompare.Application.Handlers.Requester.GetRequesterRequestsHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.GetAdminStatsQuery, CharterCompare.Application.Requests.Admin.GetAdminStatsResponse>, CharterCompare.Application.Handlers.Admin.GetAdminStatsHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.GetAdminUsersQuery, CharterCompare.Application.Requests.Admin.GetAdminUsersResponse>, CharterCompare.Application.Handlers.Admin.GetAdminUsersHandler>();
builder.Services.AddScoped<CharterCompare.Application.MediatR.IRequestHandler<CharterCompare.Application.Requests.Admin.GetAdminRequestsQuery, CharterCompare.Application.Requests.Admin.GetAdminRequestsResponse>, CharterCompare.Application.Handlers.Admin.GetAdminRequestsHandler>();

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
                "http://127.0.0.1:3000"
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
                
                if (isRequester)
                {
                    var requester = await storage.GetRequesterByExternalIdAsync(externalId, "Google");
                    if (requester == null)
                    {
                        requester = new Requester
                        {
                            Email = email,
                            Name = name ?? email,
                            ExternalId = externalId,
                            ExternalProvider = "Google",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        await storage.CreateRequesterAsync(requester);
                        logger.LogInformation("Created new requester: {RequesterId}", requester.Id);
                    }
                    else
                    {
                        requester.LastLoginAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(name) && requester.Name != name)
                        {
                            requester.Name = name;
                        }
                        await storage.UpdateRequesterAsync(requester);
                        await storage.SaveChangesAsync();
                        logger.LogInformation("Updated existing requester: {RequesterId}", requester.Id);
                    }

                    var sessionClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, requester.Id.ToString()),
                        new Claim(ClaimTypes.Email, requester.Email),
                        new Claim(ClaimTypes.Name, requester.Name),
                        new Claim("RequesterId", requester.Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Principal = new ClaimsPrincipal(claimsIdentity);
                    context.Properties.RedirectUri = $"{frontendUrl}/requester/dashboard";
                }
                else
                {
                    var operatorEntity = await storage.GetOperatorByExternalIdAsync(externalId, "Google");
                    if (operatorEntity == null)
                    {
                        operatorEntity = new Operator
                        {
                            Email = email,
                            Name = name ?? email,
                            ExternalId = externalId,
                            ExternalProvider = "Google",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        await storage.CreateOperatorAsync(operatorEntity);
                        logger.LogInformation("Created new operator: {OperatorId}", operatorEntity.Id);
                    }
                    else
                    {
                        operatorEntity.LastLoginAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(name) && operatorEntity.Name != name)
                        {
                            operatorEntity.Name = name;
                        }
                        await storage.UpdateOperatorAsync(operatorEntity);
                        await storage.SaveChangesAsync();
                        logger.LogInformation("Updated existing operator: {OperatorId}", operatorEntity.Id);
                    }

                    var sessionClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, operatorEntity.Id.ToString()),
                        new Claim(ClaimTypes.Email, operatorEntity.Email),
                        new Claim(ClaimTypes.Name, operatorEntity.Name),
                        new Claim("ProviderId", operatorEntity.Id.ToString())
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
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // For SQL Server, use EnsureCreated() which will create the schema based on DbContext
        // In production, you should use EF Core Migrations instead
        if (!dbContext.Database.CanConnect())
        {
            logger.LogInformation("Database does not exist. Creating database...");
            dbContext.Database.EnsureCreated();
            logger.LogInformation("Database created successfully.");
        }
        else
        {
            // Check if tables exist, if not, create them
            var tablesExist = false;
            try
            {
                // Try to query a table to see if schema exists
                dbContext.Database.ExecuteSqlRaw("SELECT TOP 1 Id FROM Providers");
                tablesExist = true;
            }
            catch
            {
                tablesExist = false;
            }

            if (!tablesExist)
            {
                logger.LogInformation("Database exists but tables are missing. Creating schema...");
                dbContext.Database.EnsureCreated();
                logger.LogInformation("Database schema created successfully.");
            }
            else
            {
                // Check and add missing columns if needed (for existing databases)
                try
                {
                    var hasPasswordHash = false;
                    try
                    {
                        dbContext.Database.ExecuteSqlRaw("SELECT PasswordHash FROM Providers WHERE 1=0");
                        hasPasswordHash = true;
                    }
                    catch { hasPasswordHash = false; }

                    if (!hasPasswordHash)
                    {
                        logger.LogWarning("Adding PasswordHash column to Providers table...");
                        dbContext.Database.ExecuteSqlRaw("ALTER TABLE Providers ADD PasswordHash NVARCHAR(MAX) NULL;");
                        logger.LogInformation("PasswordHash column added successfully.");
                    }

                    var hasIsAdmin = false;
                    try
                    {
                        dbContext.Database.ExecuteSqlRaw("SELECT IsAdmin FROM Providers WHERE 1=0");
                        hasIsAdmin = true;
                    }
                    catch { hasIsAdmin = false; }

                    if (!hasIsAdmin)
                    {
                        logger.LogWarning("Adding IsAdmin column to Providers table...");
                        dbContext.Database.ExecuteSqlRaw("ALTER TABLE Providers ADD IsAdmin BIT NOT NULL DEFAULT 0;");
                        logger.LogInformation("IsAdmin column added successfully.");
                    }

                    var hasRequestersTable = false;
                    try
                    {
                        dbContext.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM Requesters WHERE 1=0");
                        hasRequestersTable = true;
                    }
                    catch { hasRequestersTable = false; }

                    if (!hasRequestersTable)
                    {
                        logger.LogWarning("Requesters table does not exist. Creating...");
                        dbContext.Database.EnsureCreated(); // This will create missing tables
                        logger.LogInformation("Requesters table created successfully.");
                    }

                    var hasRequesterId = false;
                    try
                    {
                        dbContext.Database.ExecuteSqlRaw("SELECT RequesterId FROM CharterRequests WHERE 1=0");
                        hasRequesterId = true;
                    }
                    catch { hasRequesterId = false; }

                    if (!hasRequesterId)
                    {
                        logger.LogWarning("Adding RequesterId column to CharterRequests table...");
                        dbContext.Database.ExecuteSqlRaw("ALTER TABLE CharterRequests ADD RequesterId INT NULL;");
                        logger.LogInformation("RequesterId column added successfully.");
                    }
                }
                catch (Exception schemaEx)
                {
                    logger.LogWarning(schemaEx, "Could not check/update schema. This is normal for new databases.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing database.");
        // Don't delete and recreate in production - just log the error
        if (app.Environment.IsDevelopment())
        {
            try
            {
                logger.LogWarning("Development mode: Attempting to recreate database...");
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
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
