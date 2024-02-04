using Iot.Device.Graphics;
using Microsoft.Extensions.Caching.Distributed;
using OrangePi.Common.Services;
using OrangePi.Display.Status.Service.Extensions;
using OrangePi.Display.Status.Service.Models;
using System.Text;
using System.Text.Json;

namespace OrangePi.Display.Status.Service.InfoServices
{
    public class SsdInfoService : IInfoService
    {
        private readonly IProcessRunner _processRunner;
        private readonly ITemperatureReader _temperatureReader;
        private readonly string _driveMount;
        private readonly ILogger<SsdInfoService> _logger;
        private readonly IDistributedCache _cache;
        private readonly string _cacheKey;
        private readonly TimeSpan _cacheDuration;
        public SsdInfoService(
            IProcessRunner processRunner,
            IEnumerable<ITemperatureReader> temperatureReaders,
            ILogger<SsdInfoService> logger,
            IDistributedCache cache,
            string driveMount,
            int cacheDurationSeconds
            )
        {
            _logger = logger;
            _processRunner = processRunner;
            _temperatureReader = temperatureReaders.Single(r => r.GetType() == typeof(SsdTemperatureReader));
            _driveMount = driveMount;
            _cache = cache;
            _cacheKey = Guid.NewGuid().ToString();
            if (cacheDurationSeconds == 0)
                _cacheDuration = TimeSpan.Zero;
        }

        public SsdInfoService(
            IProcessRunner processRunner,
            IEnumerable<ITemperatureReader> temperatureReaders,
            ILogger<SsdInfoService> logger,
            IDistributedCache cache,
            string driveMount
            ):this(processRunner,temperatureReaders,logger,cache,driveMount,0)
        {

        }

        public string Label => "SSD";

        public async Task<BitmapImage> GetInfoDisplay(int screenWidth, int screenHeight, string fontName, int fontSize)
        {
            return await this.GetDisplay(screenWidth, screenHeight, fontName, fontSize);
        }

        public async Task<StatusValue> GetValue()
        {
            if (_cacheDuration != TimeSpan.Zero)
            {
                StatusValue? cachedValue = null;
                var cachedValueRaw = await _cache.GetStringAsync(_cacheKey);
                if (cachedValueRaw != null)
                    cachedValue = JsonSerializer.Deserialize<StatusValue>(cachedValueRaw);

                if (cachedValue != null)
                    return cachedValue;
            }

            #region Fetch values
            double fsUsage = 0;
            try
            {
                fsUsage = await _processRunner.RunAsync<double>("/bin/bash", $"-c \"df -H {_driveMount} --output=pcent | sed -e /Use%/d | grep -oP '(\\d+(\\.\\d+)?(?=%))'\"");
                fsUsage = Math.Round(fsUsage, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                fsUsage = 0;
            }

            double ssdTemp = 0;
            try
            {
                ssdTemp = await _temperatureReader.GetTemperature();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ssdTemp = 0;
            }

            var result = new StatusValue(
                valueText: $"{fsUsage.ToString("0.0")}%",
                value: fsUsage,
                note: $"{ssdTemp}°C");
            #endregion

            if (_cacheDuration != TimeSpan.Zero)
            {
                await _cache.SetAsync(
                   _cacheKey,
                   Encoding.UTF8.GetBytes(JsonSerializer.Serialize<StatusValue>(result)),
                   new DistributedCacheEntryOptions
                   {
                       AbsoluteExpirationRelativeToNow = _cacheDuration
                   });
            }

            return result;
        }
    }
}
