using Absolute.Cinema.IdentityService.Configuration;
using Absolute.Cinema.IdentityService.DataContext;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Services;
using System.Text;
using Absolute.Cinema.IdentityService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Postgres database
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Configure Redis database
builder.Services.AddSingleton<RedisCacheService>();

//Configure JWT Authentication and Authorization
var accessTokenSecretKey = builder.Configuration["TokenSettings:AccessToken:SecretKey"];
var accessTokenIssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessTokenSecretKey));

var confirmationTokenSecretKey = builder.Configuration["TokenSettings:ConfirmationToken:SecretKey"];
var confirmationTokenIssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(confirmationTokenSecretKey));

var validIssuer = builder.Configuration["TokenSettings:Common:Issuer"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("AccessToken",options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = accessTokenIssuerSigningKey,
            
            ValidateIssuer = true,
            ValidIssuer = validIssuer,
            
            ValidateLifetime = true
        };
    })
    .AddJwtBearer("ConfirmationToken", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = confirmationTokenIssuerSigningKey,

            ValidateIssuer = true,
            ValidIssuer = validIssuer,

            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Mails
builder.Services.Configure<MailConfiguration>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddTransient<IMailService, MailService>();

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
