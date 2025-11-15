using GlosterIktato.API.Services.Interfaces;

namespace GlosterIktato.API.BackgroundServices
{
    /// <summary>
    /// Background service: 30 másodpercenként ellenőrzi a feldolgozás alatt lévő Dataxo dokumentumokat
    /// </summary>
    public class DataxoPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataxoPollingService> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

        public DataxoPollingService(
            IServiceProvider serviceProvider,
            ILogger<DataxoPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Dataxo Polling Service started");

            // Várunk egy kicsit az indulás után (hogy az app teljesen inicializálódjon)
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("Running Dataxo polling cycle");

                    // Scoped service-t kell használni (DbContext miatt)
                    using var scope = _serviceProvider.CreateScope();
                    var dataxoService = scope.ServiceProvider.GetRequiredService<IDataxoService>();

                    await dataxoService.ProcessPendingDocumentsAsync();

                    _logger.LogDebug("Dataxo polling cycle completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Dataxo polling cycle");
                }

                // Várunk a következő ciklusig
                await Task.Delay(_pollInterval, stoppingToken);
            }

            _logger.LogInformation("Dataxo Polling Service stopped");
        }
    }
}