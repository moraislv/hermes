using StackExchange.Redis;

namespace hermes;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var configurationOptions = new ConfigurationOptions
            {
                EndPoints =
                {
                    "localhost:6379"
                },
                AbortOnConnectFail = false,
                ReconnectRetryPolicy = new ExponentialRetry(5000, 10000)
            };

            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
            var subscriber = connectionMultiplexer.GetSubscriber();

            await subscriber.SubscribeAsync("__keyspace@0__:*", (channel, type) =>
            {
                var key = GetKey(channel);

                switch (type)
                {
                    case "expired":
                        Console.WriteLine($"SEND TO TR KEY: {key}");
                        break;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    private static string GetKey(string channel)
    {
        var index = channel.IndexOf(':');

        if (index >= 0 && index < channel.Length - 1)
        {
            return channel[(index + 1)..];
        }

        return channel;
    }
}