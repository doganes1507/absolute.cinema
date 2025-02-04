namespace Absolute.Cinema.IdentityService.Interfaces;

public interface IKafkaProducer
{
    public Task ProduceAsync(string topic, string key, string value);
}