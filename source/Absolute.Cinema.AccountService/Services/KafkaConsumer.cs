using System.Text.Json;
using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.Models;
using Confluent.Kafka;

namespace Absolute.Cinema.AccountService.Services;

public class KafkaConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    public KafkaConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        var result = Enum.TryParse(configuration["Kafka:AutoOffsetReset"], out AutoOffsetReset autoOffsetReset);
        if (!result)
        {
            throw new Exception("Kafka Auto Offset Reset Error");
        }
        
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:GroupId"],
            AutoOffsetReset = autoOffsetReset
        };
        
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(configuration["Kafka:Topic"]);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = _consumer.Consume(stoppingToken);
            var user = JsonSerializer.Deserialize<KafkaUserCreate>(consumeResult.Message.Value);
            if (user == null)
            {
                throw new Exception("Kafka Consumer Error");
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
            await dbContext.Users.AddAsync(new User
            {
                Id = user.Id,
                EmailAddress = user.EmailAddress
            }, stoppingToken);
                
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}