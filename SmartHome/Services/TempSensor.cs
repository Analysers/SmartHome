using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SmartHome.Models;

namespace SmartHome.Services
{
    public interface ITempSensor
    {
        void StartService(CancellationToken cancellationToken = default);
    }

    public class TempSensor : ITempSensor, IDisposable
    {
        public TempSensor(IConfiguration configuration, ILogger<TempSensor> logger,
            DbContextOptions<Database> dbOptions, IServiceDiscovery serviceDiscovery)
        {
            Configuration = configuration;
            Logger = logger;
            DbOptions = dbOptions;
            ServiceDiscovery = serviceDiscovery;
        }

        private IConfiguration Configuration { get; }
        private DbContextOptions<Database> DbOptions { get; }
        private ILogger<TempSensor> Logger { get; }
        private IServiceDiscovery ServiceDiscovery { get; }
        private HttpClient HttpClient { get; } = new HttpClient();

        public void Dispose()
        {
            HttpClient?.Dispose();
        }

        public void StartService(CancellationToken cancellationToken = default)
        {
            Task.Run(async () =>
            {
                try
                {
                    var hostName = "";
                    while (true)
                    {
                        var discoverEvent = ServiceDiscovery.DiscoverService("_tempsensor._tcp.local");
                        discoverEvent += (sender, e) => hostName = e.IPAddress.ToString();
                        while (string.IsNullOrEmpty(hostName)) await Task.Delay(1000, cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        while (true)
                        {
                            try
                            {
                                var jsonString = await HttpClient.GetStringAsync($"http://{hostName}/sensor");
                                var json = JObject.Parse(jsonString);
                                Logger.LogDebug(json.ToString());

                                using (var database = new Database(DbOptions))
                                {
                                    database.Temperature.Add(new Temperature
                                        {TemperatureCelsius = json["temperature"].ToObject<float>()});
                                    database.Humidity.Add(new Humidity
                                        {HumidityPercent = json["humidity"].ToObject<float>()});
                                    await database.SaveChangesAsync(cancellationToken);
                                }
                            }
                            catch (Exception e)
                            {
                                var resetHost = false;
                                switch (e)
                                {
                                    case UriFormatException _:
                                        Logger.LogWarning(e,
                                            $"Invalid Url http://{hostName}/sensor; Resetting hostname");
                                        resetHost = true;
                                        break;
                                    case HttpRequestException _:
                                        Logger.LogWarning(e, "Failed to get response; Resetting hostname");
                                        resetHost = true;
                                        break;
                                    default:
                                        Logger.LogWarning(e, "Failed to fetch sensor data");
                                        break;
                                }

                                if (resetHost)
                                {
                                    hostName = "";
                                    break;
                                }
                            }
                            finally
                            {
                                await Task.Delay(5000, cancellationToken);
                            }

                            if (cancellationToken.IsCancellationRequested)
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogCritical(e, "Unexpected error");
                }
            }, cancellationToken);
        }
    }
}