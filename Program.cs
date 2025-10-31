using Logistic_Shipment_tracker.Data;
using Logistic_Shipment_tracker.Middleware;
using Logistic_Shipment_tracker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);


builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "")),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });



// Add Authorization
builder.Services.AddAuthorization();


builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Development: Allow HTTP origins
                policy
                    .WithOrigins("https://logistic-project-frontend.vercel.app", "http://localhost:5173", "https://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Required for cookies
            }
            else
            {
                // Production: Only HTTPS origins (replace with your actual production URLs)
                policy
                    .WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                    //.AllowCredentials(); // Required for cookies
            }
        }
    );
});


builder.Services.AddHttpClient();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<INotificationService , NotificationService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<IDriverAssignmentService, DriverAssignmentService>();
builder.Services.AddScoped<IDistanceService, NominatimDistanceService>();
builder.Services.AddScoped<IReportService ,ReportService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Logistic Shipment Tracker API",
            Version = "v1",
            Description = "API for managing shipments, tracking, and logistics operations",
        }
    );

    // Define the Bearer Auth scheme
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http, // <-- should be Http, not ApiKey
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token in this format: Bearer {your token}",
        }
    );

    // Make sure Swagger UI requires a Bearer token for authorized endpoints
    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                    Scheme = "bearer",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            },
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseRouting(); // Make routing explicit

// CORS must come before authentication
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseCookieToHeader();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
