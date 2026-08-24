using System.Text;
using ConnectHub.API.Filters;
using ConnectHub.API.Hubs;
using ConnectHub.API.Middleware;
using ConnectHub.API.Services;
using ConnectHub.BLL.Extensions;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers
builder.Services.AddScoped<FluentValidationFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<FluentValidationFilter>());

// 2. Add DAL Services (DbContext, Identity, Repositories, UnitOfWork)
builder.Services.AddDalServices(builder.Configuration);

// 3. Add BLL Services (FluentValidation, AutoMapper, Business Services, Storage)
builder.Services.AddBllServices();

// 4. Register SignalR & Real-time Notification Service
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();

// 5. Configure JWT Bearer Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ConnectHubSuperSecretSigningKeyForJwtTokensMustBeAtLeast32BytesLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ConnectHub";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ConnectHubClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Support token in query string for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 6. Configure Swagger/OpenAPI with JWT Bearer Authentication Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ConnectHub API",
        Version = "v1",
        Description = "ConnectHub Social Collaboration Platform RESTful Web API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste the JWT access token only. Swagger automatically sends: Bearer {token}.",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// 7. Configure Middleware Pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConnectHub API v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 8. Map SignalR Hub Endpoints
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<GroupHub>("/hubs/groups");

app.Run();
