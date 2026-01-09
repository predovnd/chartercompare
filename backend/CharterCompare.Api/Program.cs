using CharterCompare.Api.Models;
using CharterCompare.Api.Services;
using CharterCompare.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=chartercompare.db"));

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
    // Store authentication state in cookies
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
        // Explicitly set the callback path to match what we're using
        options.CallbackPath = "/api/auth/google-callback"; // Default for operators
        // Save tokens for later use if needed
        options.SaveTokens = true;
        // Configure cookie settings for OAuth state
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.Name = ".AspNetCore.CharterCompare.Correlation.Google";
        options.CorrelationCookie.Path = "/";
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        
        // Set redirect URI after successful authentication
        var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";
        options.Events.OnCreatingTicket = async context =>
        {
            // This runs after Google authentication succeeds
            var claims = context.Principal?.Claims.ToList();
            var email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;
            var externalId = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(externalId))
            {
                // Get services from the request's service provider
                var providerService = context.HttpContext.RequestServices.GetRequiredService<IProviderService>();
                var requesterService = context.HttpContext.RequestServices.GetRequiredService<IRequesterService>();
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                // Determine user type from authentication properties
                // The userType is stored in Properties.Items when the Challenge is initiated
                var isRequester = context.Properties.Items.TryGetValue("userType", out var userTypeValue) && userTypeValue == "requester";
                
                if (isRequester)
                {
                    // Handle requester authentication
                    var requester = await requesterService.GetRequesterByExternalIdAsync(externalId, "Google");
                    if (requester == null)
                    {
                        requester = await requesterService.CreateRequesterAsync(email, name ?? email, externalId, "Google");
                        logger.LogInformation("Created new requester: {RequesterId}", requester.Id);
                    }
                    else
                    {
                        requester.LastLoginAt = DateTime.UtcNow;
                        dbContext.Requesters.Update(requester);
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("Updated existing requester: {RequesterId}", requester.Id);
                    }

                    // Update requester info if name changed
                    if (!string.IsNullOrEmpty(name) && requester.Name != name)
                    {
                        requester.Name = name;
                        dbContext.Requesters.Update(requester);
                        await dbContext.SaveChangesAsync();
                    }

                    // Replace the Google claims with our application claims
                    var sessionClaims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, requester.Id.ToString()),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, requester.Email),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, requester.Name),
                        new System.Security.Claims.Claim("RequesterId", requester.Id.ToString())
                    };

                    var claimsIdentity = new System.Security.Claims.ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Principal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
                    
                    // Set redirect URI
                    context.Properties.RedirectUri = $"{frontendUrl}/requester/dashboard";
                }
                else
                {
                    // Handle operator/provider authentication (existing logic)
                    var provider = await providerService.GetProviderByExternalIdAsync(externalId, "Google");
                    if (provider == null)
                    {
                        provider = await providerService.CreateProviderAsync(email, name ?? email, externalId, "Google");
                        logger.LogInformation("Created new provider: {ProviderId}", provider.Id);
                    }
                    else
                    {
                        provider.LastLoginAt = DateTime.UtcNow;
                        dbContext.Providers.Update(provider);
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("Updated existing provider: {ProviderId}", provider.Id);
                    }

                    // Update provider info if name changed
                    if (!string.IsNullOrEmpty(name) && provider.Name != name)
                    {
                        provider.Name = name;
                        dbContext.Providers.Update(provider);
                        await dbContext.SaveChangesAsync();
                    }

                    // Replace the Google claims with our application claims
                    var sessionClaims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, provider.Id.ToString()),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, provider.Email),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, provider.Name),
                        new System.Security.Claims.Claim("ProviderId", provider.Id.ToString())
                    };

                    var claimsIdentity = new System.Security.Claims.ClaimsIdentity(sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Principal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);
                    
                    // Set redirect URI
                    context.Properties.RedirectUri = $"{frontendUrl}/provider/dashboard";
                }
            }
            
            await Task.CompletedTask;
        };
    });
}

// Register services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IRequesterService, RequesterService>();
builder.Services.AddMemoryCache(); // For session storage (can be replaced with Redis in production)

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Check if database exists and if schema needs updating
        if (dbContext.Database.CanConnect())
        {
            // Try to check if PasswordHash column exists
            var hasPasswordHash = false;
            var hasIsAdmin = false;
            try
            {
                dbContext.Database.ExecuteSqlRaw("SELECT PasswordHash FROM Providers LIMIT 1");
                hasPasswordHash = true;
            }
            catch
            {
                hasPasswordHash = false;
            }
            
            try
            {
                dbContext.Database.ExecuteSqlRaw("SELECT IsAdmin FROM Providers LIMIT 1");
                hasIsAdmin = true;
            }
            catch
            {
                hasIsAdmin = false;
            }
            
            if (!hasPasswordHash)
            {
                logger.LogWarning("Database schema is outdated. Adding PasswordHash column...");
                dbContext.Database.ExecuteSqlRaw(@"
                    ALTER TABLE Providers ADD COLUMN PasswordHash TEXT;
                ");
                logger.LogInformation("PasswordHash column added successfully.");
            }
            
            if (!hasIsAdmin)
            {
                logger.LogWarning("Database schema is outdated. Adding IsAdmin column...");
                dbContext.Database.ExecuteSqlRaw(@"
                    ALTER TABLE Providers ADD COLUMN IsAdmin INTEGER NOT NULL DEFAULT 0;
                ");
                logger.LogInformation("IsAdmin column added successfully.");
            }
            
            // Check if Requesters table exists
            var hasRequestersTable = false;
            try
            {
                dbContext.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM Requesters LIMIT 1");
                hasRequestersTable = true;
            }
            catch
            {
                hasRequestersTable = false;
            }
            
            if (!hasRequestersTable)
            {
                logger.LogWarning("Database schema is outdated. Creating Requesters table...");
                dbContext.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS Requesters (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Email TEXT NOT NULL UNIQUE,
                        Name TEXT NOT NULL,
                        Phone TEXT,
                        ExternalId TEXT NOT NULL,
                        ExternalProvider TEXT NOT NULL,
                        PasswordHash TEXT,
                        CreatedAt TEXT NOT NULL,
                        LastLoginAt TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        UNIQUE(ExternalId, ExternalProvider)
                    );
                ");
                logger.LogInformation("Requesters table created successfully.");
            }
            
            // Check if RequesterId column exists in CharterRequests
            var hasRequesterId = false;
            try
            {
                dbContext.Database.ExecuteSqlRaw("SELECT RequesterId FROM CharterRequests LIMIT 1");
                hasRequesterId = true;
            }
            catch
            {
                hasRequesterId = false;
            }
            
            if (!hasRequesterId)
            {
                logger.LogWarning("Database schema is outdated. Adding RequesterId column...");
                dbContext.Database.ExecuteSqlRaw(@"
                    ALTER TABLE CharterRequests ADD COLUMN RequesterId INTEGER;
                ");
                logger.LogInformation("RequesterId column added successfully.");
            }
        }
        else
        {
            dbContext.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing database. Attempting to recreate...");
        // If migration fails, delete and recreate (development only)
        try
        {
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


// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must be before other middleware
app.UseCors("AllowLocalhost");

// Log CORS requests in development
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

// Authentication and Authorization must be before MapControllers
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
