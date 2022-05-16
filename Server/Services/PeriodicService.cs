namespace ZapWord.Server.Services;

public class PeriodicService : IHostedService, IDisposable
{
    private const int TIMER_SECONDS = 60;
    private readonly ILogger<PeriodicService> _logger;
    private readonly IGameFabric _gameFabric;
    private Timer _timer = null!;

    public PeriodicService(ILogger<PeriodicService> logger, IGameFabric gameFabric)
    {
        _logger = logger;
        _gameFabric = gameFabric;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(async s => await DoWork(s), null, TimeSpan.Zero, TimeSpan.FromSeconds(TIMER_SECONDS));
        return Task.CompletedTask;
    }

    private async Task DoWork(object? state)
    {
        try
        {
            await _gameFabric.EnqueueGame();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, nameof(DoWork));
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}