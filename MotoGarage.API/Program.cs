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
builder.Services.AddSwaggerGen();


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


// =========================
// Authentication Services
// =========================

builder.Services.AddScoped<
    ITokenService,
    TokenService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();


// =========================
// Application Services
// =========================

builder.Services.AddScoped<
    IVehicleService,
    VehicleService>();

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