using Microsoft.Extensions.Options;
using OrangePi.PWM.Service.Models;
using OrangePi.PWM.Service.Services;

namespace OrangePi.PWM.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptionsMonitor<ServiceConfiguration> _serviceConfigMonitor;
        private readonly IProcessRunner _processRunner;
        public Worker(
            ILogger<Worker> logger,
            IOptionsMonitor<ServiceConfiguration> serviceConfigMonitor,
            IProcessRunner processRunner)
        {
            _logger = logger;
            _serviceConfigMonitor = serviceConfigMonitor;
            _processRunner = processRunner;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int previousSpeed = 0;
            await _processRunner.RunAsync("gpio", "mode", _serviceConfigMonitor.CurrentValue.wPi.ToString(), "pwm"));

            while (!stoppingToken.IsCancellationRequested)
            {
                var temeratureCheckOutput = await _processRunner.RunAsync("cat", "/sys/class/thermal/thermal_zone0/temp");

                if (double.TryParse(temeratureCheckOutput, out double temperature))
                {
                    if (temperature > 0)
                        temperature = temperature / 1000;

                    var speed = _serviceConfigMonitor.CurrentValue.TemperatureConfigurations.OrderBy(r => r.Temperature).Where(r => r.Temperature >= temperature).FirstOrDefault()?.Speed;
                    speed = speed ?? 0;

                    if (previousSpeed != speed)
                    {
                        previousSpeed = speed.Value;
                        _logger.LogInformation($"Updating PWM Temperature: {temperature}; Speed: {speed}");

                        await _processRunner.RunAsync("gpio", "pwm", _serviceConfigMonitor.CurrentValue.wPi.ToString(), speed.ToString());
                    }
                }

                Task.Delay(TimeSpan.FromSeconds(_serviceConfigMonitor.CurrentValue.IntervalSeconds)).Wait();
            }
        }
    }
}