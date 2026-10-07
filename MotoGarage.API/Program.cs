using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MotoGarage.API.Hubs;
using MotoGarage.API.Middleware;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Interfaces.Services.Auth;
using MotoGarage.Application.Mapping;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Email;
using MotoGarage.Infrastructure.Identity;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Storage;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = StartupConfiguration.Validate(builder.Configuration);


// =========================
// Controllers
// =========================

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions
            .Converters
            .Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MotoGarage API",
        Version = "v1"
    });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT token daxil edin."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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


// =========================
// Database
// =========================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"),
        sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), new[] { 1205 }));
});


// =========================
// Identity
// =========================

builder.Services
    .AddIdentityCore<User>(options =>
    {
        // Password
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // User
        options.User.RequireUniqueEmail = true;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);

        options.Lockout.MaxFailedAccessAttempts = 5;

        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


// =========================
// JWT Authentication
// =========================



var jwtIssuer =
    builder.Configuration["Jwt:Issuer"];

var jwtAudience =
    builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };

        options.Events =
            new JwtBearerEvents
            {
                // =========================
                // SignalR token
                // =========================
                OnMessageReceived = context =>
                {
                    var accessToken =
                        context.Request.Query[
                            "access_token"];

                    var path =
                        context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },

                // =========================
                // Blocked user check
                // =========================
                OnTokenValidated = async context =>
                {
                    var userIdValue =
                        context.Principal?
                            .FindFirstValue(
                                ClaimTypes.NameIdentifier);

                    if (string.IsNullOrWhiteSpace(userIdValue) ||
                        !int.TryParse(userIdValue, out var userId))
                    {
                        context.Fail(
                            "İstifadəçi məlumatı tapılmadı.");

                        return;
                    }

                    var userManager =
                        context.HttpContext
                            .RequestServices
                            .GetRequiredService<UserManager<User>>();

                    var user =
                        await userManager.FindByIdAsync(
                            userId.ToString());

                    if (user is null)
                    {
                        context.Fail(
                            "İstifadəçi tapılmadı.");

                        return;
                    }

                    var isBlocked = user.IsAdminBlocked ||
                        (user.LockoutEnd.HasValue &&
                         user.LockoutEnd.Value > DateTimeOffset.UtcNow);

                    if (isBlocked)
                    {
                        context.Fail(
                            "İstifadəçi bloklanıb.");

                        return;
                    }

                    if (!user.EmailConfirmed)
                    {
                        context.Fail("Email təsdiqlənməyib.");
                        return;
                    }

                    var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
                    if (!string.Equals(role, user.Role.ToString(), StringComparison.Ordinal))
                        context.Fail("Token rolu istifadəçinin cari rolu ilə uyğun deyil.");
                }
            };
    });


// =========================
// Authorization
// =========================

builder.Services.AddAuthorization();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IUserConnectionRegistry, UserConnectionRegistry>();
builder.Services.AddScoped<IDashboardQuery, DashboardQuery>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    foreach (var setting in new[]
    {
        (Name: "auth-login", Limit: 10, Minutes: 1),
        (Name: "auth-register", Limit: 5, Minutes: 10),
        (Name: "auth-verify", Limit: 20, Minutes: 1),
        (Name: "auth-resend", Limit: 5, Minutes: 10),
        (Name: "image-upload", Limit: 20, Minutes: 1)
    })
    {
        options.AddPolicy(setting.Name, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = setting.Limit,
                    Window = TimeSpan.FromMinutes(setting.Minutes),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }
});


// =========================
// AutoMapper
// =========================

builder.Services.AddAutoMapper(
    cfg => { },
    typeof(MappingProfile).Assembly);


// =========================
// Generic Repository
// =========================

builder.Services.AddScoped(
    typeof(IRepository<>),
    typeof(Repository<>));


// =========================
// Repositories
// =========================

builder.Services.AddScoped<
    IEmailVerificationCodeRepository,
    EmailVerificationCodeRepository>();

builder.Services.AddScoped<
    IVehicleRepository,
    VehicleRepository>();

builder.Services.AddScoped<
    IServiceRecordRepository,
    ServiceRecordRepository>();

builder.Services.AddScoped<
    IFuelRecordRepository,
    FuelRecordRepository>();

builder.Services.AddScoped<
    IExpenseRepository,
    ExpenseRepository>();

builder.Services.AddScoped<
    IReminderRepository,
    ReminderRepository>();

builder.Services.AddScoped<
    IMechanicRepository,
    MechanicRepository>();

builder.Services.AddScoped<
    IAppointmentRepository,
    AppointmentRepository>();

builder.Services.AddScoped<
    IReviewRepository,
    ReviewRepository>();

builder.Services.AddScoped<
    IStoreRepository,
    StoreRepository>();

builder.Services.AddScoped<
    IProductRepository,
    ProductRepository>();

builder.Services.AddScoped<
    IOrderRepository,
    OrderRepository>();

builder.Services.AddScoped<
    IOrderItemRepository,
    OrderItemRepository>();

builder.Services.AddScoped<
    IConversationRepository,
    ConversationRepository>();

builder.Services.AddScoped<
    IMessageRepository,
    MessageRepository>();

builder.Services.AddScoped<
    IRefreshTokenRepository,
    RefreshTokenRepository>();

builder.Services.AddScoped<
    INotificationRepository,
    NotificationRepository>();


// =========================
// Authentication Services
// =========================

builder.Services.AddScoped<
    ITokenService,
    TokenService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    IEmailService,
    SmtpEmailService>();


// =========================
// Application Services
// =========================

builder.Services.AddScoped<
    IAdminService,
    AdminService>();

builder.Services.AddScoped<
    IUserProfileService,
    UserProfileService>();

builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();

builder.Services.AddScoped<
    IVehicleService,
    VehicleService>();

builder.Services.AddScoped<
    INotificationService,
    NotificationService>();

builder.Services.AddScoped<
    IServiceRecordService,
    ServiceRecordService>();

builder.Services.AddScoped<
    IFuelRecordService,
    FuelRecordService>();

builder.Services.AddScoped<
    IExpenseService,
    ExpenseService>();

builder.Services.AddScoped<
    IReminderService,
    ReminderService>();

builder.Services.AddScoped<
    IMechanicService,
    MechanicService>();

builder.Services.AddScoped<
    IAppointmentService,
    AppointmentService>();

builder.Services.AddScoped<
    IReviewService,
    ReviewService>();

builder.Services.AddScoped<
    IStoreService,
    StoreService>();

builder.Services.AddScoped<
    IProductService,
    ProductService>();

builder.Services.AddScoped<
    IOrderService,
    OrderService>();

builder.Services.AddScoped<
    IChatService,
    ChatService>();

builder.Services.AddScoped<
    IImageService,
    CloudinaryImageService>();


// =========================
// CORS
// React frontend
// =========================

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment() ? new[] { "http://localhost:5173" } : Array.Empty<string>());
if (!builder.Environment.IsDevelopment() && corsOrigins.Length == 0)
    throw new InvalidOperationException("Production üçün Cors:Origins konfiqurasiya edilməlidir.");
if (corsOrigins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
    || (uri.Scheme != "http" && uri.Scheme != "https") || origin.Contains('*')))
    throw new InvalidOperationException("Cors:Origins konkret HTTP/HTTPS origin-lərindən ibarət olmalıdır.");

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactClient",
        policy =>
        {
            policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


// =========================
// SignalR
// =========================

builder.Services.AddSignalR();


var app = builder.Build();

await AdminSeeder.SeedAsync(
    app.Services,
    app.Configuration);


// =========================
// Middleware Pipeline
// =========================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Global Exception Handling
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("ReactClient");

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();


// =========================
// Endpoints
// =========================

app.MapControllers();

app.MapHub<ChatHub>(
    "/hubs/chat", options => options.CloseOnAuthenticationExpiration = true);


app.Run();