using System.Text;
using Absolute.Cinema.IdentityService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Absolute.Cinema.IdentityService.Configuration;
using Absolute.Cinema.IdentityService.DataObjects.AdminController;
using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Services;
using Absolute.Cinema.IdentityService.Models;
using Absolute.Cinema.IdentityService.Repositories;
using Absolute.Cinema.IdentityService.Validators.AdminController;
using Absolute.Cinema.IdentityService.Validators.IdentityController;
using Absolute.Cinema.Shared.Interfaces;
using Absolute.Cinema.Shared.KafkaEvents;
using Absolute.Cinema.Shared.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using KafkaFlow.Serializer;
using KafkaFlow;
using StackExchange.Redis;
using Role = Absolute.Cinema.IdentityService.Models.Role;

var builder = WebApplication.CreateBuilder(args);

// Configure Postgres database
builder.Services.AddDbContext<ApplicationDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    .UseLazyLoadingProxies());

// Configure Repository
builder.Services.AddScoped<IRepository<User>, EntityFrameworkRepository<User>>();
builder.Services.AddScoped<IRepository<Role>, EntityFrameworkRepository<Role>>();

// Configure Redis database
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Configure Email sender
builder.Services.Configure<MailConfiguration>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddTransient<IMailService, MailService>();

// Configure Kafka producer
builder.Services.AddKafka(
    kafka => kafka
        .AddCluster(cluster => cluster
            .WithBrokers([builder.Configuration["Kafka:BrokerAddress"]])
            .CreateTopicIfNotExists(builder.Configuration["Kafka:Topic"])
            //.AddProducer<>
            .AddProducer(builder.Configuration["Kafka:ProducerName"], producer => producer
                .DefaultTopic(builder.Configuration["Kafka:Topic"])
                //.WithAcks(Acks.Leader)
                .AddMiddlewares(middleware => middleware
                    .AddSingleTypeSerializer<SyncUserEvent, JsonCoreSerializer>()
                )
            ))
);

// Configure Token provider
builder.Services.AddTransient<ITokenProvider, TokenProvider>();

// Configure Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddTransient<IValidator<SendEmailCodeDto>, SendEmailCodeDtoValidator>();
builder.Services.AddTransient<IValidator<UpdatePasswordDto>, UpdatePasswordDtoValidator>();
builder.Services.AddTransient<IValidator<UpdateEmailAddressDto>, UpdateEmailAddressDtoValidator>();

builder.Services.AddTransient<IValidator<CreateUserDto>, CreateUserDtoValidator>();
builder.Services.AddTransient<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();

// Configure JWT Authentication and Authorization
var secretKey = builder.Configuration["TokenSettings:AccessToken:SecretKey"];
var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
var validIssuer = builder.Configuration["TokenSettings:Common:Issuer"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = issuerSigningKey,

            ValidateAudience = false,

            ValidateIssuer = true,
            ValidIssuer = validIssuer,

            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
    .AddPolicy("UserPolicy", policy => policy.RequireRole("User"));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();