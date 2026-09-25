using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TeamContributionManagementSystem.API.Middleware;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Infrastructure;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/api-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddMemoryCache();

builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssembly(typeof(CommonConstants).Assembly);

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = CommonConstants.ApiConfig.VersionGroupFormat;
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(CommonConstants.ApiConfig.V1, new OpenApiInfo { Title = CommonConstants.ApiConfig.ApiTitle, Version = CommonConstants.ApiConfig.V1 });
    options.AddSecurityDefinition(CommonConstants.Auth.Bearer, new OpenApiSecurityScheme
    {
        Name = CommonConstants.Auth.Authorization,
        Type = SecuritySchemeType.ApiKey,
        Scheme = CommonConstants.Auth.Bearer,
        BearerFormat = CommonConstants.Auth.Jwt,
        In = ParameterLocation.Header,
        Description = CommonConstants.Auth.BearerDescription
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = CommonConstants.Auth.Bearer
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
builder.Services.AddApplicationAndInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var allowedOrigins = builder.Configuration
    .GetSection(CommonConstants.ConfigSections.CorsAllowedOrigins)
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CommonConstants.CorsPolicies.FrontendPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Fallback: allow all localhost ports in development
            policy.SetIsOriginAllowed(origin =>
                    new Uri(origin).Host == CommonConstants.Defaults.Localhost)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var jwtSecret = builder.Configuration[CommonConstants.ConfigKeys.JwtSecret]
    ?? throw new InvalidOperationException(CommonMessages.Auth.JwtSecretNotConfigured);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration[CommonConstants.ConfigKeys.JwtIssuer],
            ValidAudience = builder.Configuration[CommonConstants.ConfigKeys.JwtAudience],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
// Disabled automatic background birthday event generation so deleted events are not continuously recreated
// builder.Services.AddHostedService<BirthdayEventHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(CommonConstants.CorsPolicies.FrontendPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks(CommonRoutes.Health);

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ApplicationDbContextSeeder>();
    await seeder.SeedAsync();
}

app.Run();
