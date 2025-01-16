using System.Text;
using Absolute.Cinema.IdentityService.DataContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Postgres database
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Configure Redis database
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

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
