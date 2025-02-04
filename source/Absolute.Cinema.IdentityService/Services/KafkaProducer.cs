using Absolute.Cinema.IdentityService.Interfaces;
using Confluent.Kafka;

namespace Absolute.Cinema.IdentityService.Services;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            // Acks = Acks.None
        };
        
        _producer = new ProducerBuilder<string, string>(config)
            .Build();
    }

    public async Task ProduceAsync(string topic, string key, string value)
    {
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = value
        });
    }
}