using System.Text;
using Absolute.Cinema.IdentityService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Absolute.Cinema.IdentityService.Configuration;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Services;
using System.Text;
using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.Models;
using Absolute.Cinema.IdentityService.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Role = Absolute.Cinema.IdentityService.Models.Role;

var builder = WebApplication.CreateBuilder(args);

// Configure Postgres database
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Configure Repository
builder.Services.AddScoped<IRepository<User>, EntityFrameworkRepository<User>>();
builder.Services.AddScoped<IRepository<Role>, EntityFrameworkRepository<Role>>();

// Configure Redis database
builder.Services.AddSingleton<RedisCacheService>();

// Configure Email sender
builder.Services.Configure<MailConfiguration>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddTransient<IMailService, MailService>();

// Configure Token provider
builder.Services.AddTransient<ITokenProvider, TokenProvider>();

//Configure JWT Authentication and Authorization
var accessTokenSecretKey = builder.Configuration["TokenSettings:AccessToken:SecretKey"];
var accessTokenIssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessTokenSecretKey));
var validIssuer = builder.Configuration["TokenSettings:Common:Issuer"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = accessTokenIssuerSigningKey,
            
            ValidateAudience = false,
          
            ValidateIssuer = true,
            ValidIssuer = validIssuer,
            
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
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
            new string[] { }
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
