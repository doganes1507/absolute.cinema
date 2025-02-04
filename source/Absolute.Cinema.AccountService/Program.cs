using System.Text;
using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.DataObjects;
using Absolute.Cinema.AccountService.Handlers;
using Absolute.Cinema.AccountService.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Postgres database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Configure Redis database
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddScoped<RedisCacheService>();

// Configure Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddTransient<IValidator<UpdatePersonalInfoDto>, UpdatePersonalInfoDtoValidator>();

// Configure JWT Authentication and Authorization
var secretKey = builder.Configuration["TokenSettings:AccessToken:SecretKey"];
var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
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

// Configure Kafka
builder.Services.AddKafkaFlowHostedService(
    kafka => kafka
        .AddCluster(cluster =>
                cluster
                    .WithBrokers(new[] { builder.Configuration["KafkaSettings:BrokerAddress"] })
                    .CreateTopicIfNotExists(builder.Configuration["KafkaSettings:TopicName"], 1, 1)
                    .AddConsumer(consumer =>
                        consumer
                            .Topic(builder.Configuration["KafkaSettings:TopicName"])
                            .WithGroupId(builder.Configuration["KafkaSettings:GroupId"])
                            .WithBufferSize(100)
                            .WithWorkersCount(3)
                            .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                            .AddMiddlewares(middlewares => middlewares
                                .AddDeserializer<JsonCoreDeserializer>()
                                .AddTypedHandlers(handlers =>
                                    handlers
                                        .AddHandler<CreateUserHandler>()
                                        .WithHandlerLifetime(InstanceLifetime.Scoped)
                                )
                            )
                    )
        ));

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

