using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MotoGarage.API.Hubs;
using MotoGarage.API.Middleware;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Interfaces.Services.Auth;
using MotoGarage.Application.Mapping;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Infrastructure.Storage;
using MotoGarage.Infrastructure.Identity;
using MotoGarage.Infrastructure.Administration;
using Microsoft.OpenApi.Models;
using MotoGarage.Infrastructure.Email;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


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
            "DefaultConnection"));
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

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT Key tapılmadı.");

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

                ValidIssuer = jwtIssuer,

                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };

        // SignalR JWT
        options.Events =
            new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken =
                        context.Request.Query[
                            "access_token"];

                    var path =
                        context.HttpContext
                            .Request.Path;

                    if (!string.IsNullOrEmpty(
                            accessToken) &&
                        path.StartsWithSegments(
                            "/hubs"))
                    {
                        context.Token =
                            accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
    });


// =========================
// Authorization
// =========================

builder.Services.AddAuthorization();


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

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactClient",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173")
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


// =========================
// Endpoints
// =========================

app.MapControllers();

app.MapHub<ChatHub>(
    "/hubs/chat");


app.Run();