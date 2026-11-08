using QuanLyTaiKhoanAPI.Interfaces;
using QuanLyTaiKhoanAPI.Extensions;
using QuanLyTaiKhoanAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using System.Text;
using QuanLyTaiKhoanAPI.Authorization;
using QuanLyTaiKhoanAPI.Data;
using QuanLyTaiKhoanAPI.Entities;
using QuanLyTaiKhoanAPI.Middlewares;
using QuanLyTaiKhoanAPI.Responses;
var builder = WebApplication.CreateBuilder(args);

#region DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection with the Supabase PostgreSQL connection string via user-secrets or ConnectionStrings__DefaultConnection.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, postgres =>
        postgres.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.Schema)));
#endregion

#region Identity
var maxFailedAccessAttempts = builder.Configuration.GetValue("Identity:Lockout:MaxFailedAccessAttempts", 5);
var defaultLockoutMinutes = builder.Configuration.GetValue("Identity:Lockout:DefaultLockoutMinutes", 15);
var passwordResetTokenMinutes = builder.Configuration.GetValue("Identity:PasswordResetTokenMinutes", 15);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = maxFailedAccessAttempts;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(defaultLockoutMinutes);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(passwordResetTokenMinutes);
});
#endregion

#region DI Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("AI", client => client.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<QuizStore>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();

if (builder.Configuration.GetValue("RefreshTokenCleanup:Enabled", true))
{
    builder.Services.AddHostedService<RefreshTokenCleanupService>();
}
#endregion

#region JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new Exception("JWT Key is missing");

if (string.IsNullOrWhiteSpace(jwtKey) ||
    jwtKey.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase) ||
    jwtKey.Contains("SUPER_SECRET", StringComparison.OrdinalIgnoreCase) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Configure Jwt:Key with a private random secret of at least 32 bytes via user-secrets, environment variables, or ignored local settings.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };
});
#endregion

#region Authorization Policies
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});
#endregion

#region Rate Limiting
var authPermitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
var authWindowMinutes = builder.Configuration.GetValue("RateLimiting:Auth:WindowMinutes", 1);
var passwordResetPermitLimit = builder.Configuration.GetValue("RateLimiting:PasswordReset:PermitLimit", 3);
var passwordResetWindowMinutes = builder.Configuration.GetValue("RateLimiting:PasswordReset:WindowMinutes", 15);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientRateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(authWindowMinutes),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));

    options.AddPolicy("password-reset", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientRateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = passwordResetPermitLimit,
                Window = TimeSpan.FromMinutes(passwordResetWindowMinutes),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));

    options.AddPolicy("ai-chat", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));
});
#endregion

#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue",
        policy =>
        {
            var configuredOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();

            if (configuredOrigins is { Length: > 0 })
            {
                policy.WithOrigins(configuredOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
                return;
            }

            var clientUrl = builder.Configuration["App:ClientUrl"];
            if (!string.IsNullOrWhiteSpace(clientUrl))
            {
                policy.WithOrigins(clientUrl)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
                return;
            }

            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException("Cors:AllowedOrigins or App:ClientUrl must be configured in production.");
            }

            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
#endregion

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => string.IsNullOrWhiteSpace(item.Key) ? "Request" : item.Key,
                item => item.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Invalid value."
                        : error.ErrorMessage)
                    .ToArray());

        var response = ApiResponse<object>.Fail(
            "Validation failed",
            "VALIDATION_ERROR",
            errors);

        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
var enableHttpsRedirection = builder.Configuration.GetValue<bool>("EnableHttpsRedirection");


#region Middleware

app.UseMiddleware<ExceptionMiddleware>();

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

// CORS should run before authentication/authorization.
app.UseRouting();
app.UseCors("AllowVue");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

#endregion

#region Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<AppDbContext>();
    if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
        await dbContext.Database.MigrateAsync();
    // Used to initialize the schema before importing existing SQLite users/roles.
    if (args.Contains("--migrate-only"))
    {
        await dbContext.Database.MigrateAsync();
        return;
    }
    await SeedAdminPermissions.SeedAsync(services);
}
#endregion

app.Run();

static string GetClientRateLimitKey(HttpContext context)
{
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return forwardedFor.Split(',')[0].Trim();
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
