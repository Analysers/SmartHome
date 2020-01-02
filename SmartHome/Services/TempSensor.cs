using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Makaretu.Dns;
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
            DbContextOptions<Database> dbOptions)
        {
            Configuration = configuration;
            Logger = logger;
            DbOptions = dbOptions;
            MDNS = new MulticastService();
            ServiceDiscovery = new ServiceDiscovery(MDNS);
        }

        private IConfiguration Configuration { get; }
        private DbContextOptions<Database> DbOptions { get; }
        private ILogger<TempSensor> Logger { get; }
        private HttpClient HttpClient { get; } = new HttpClient();
        private MulticastService MDNS { get; }
        private ServiceDiscovery ServiceDiscovery { get; }
        private string HostName { get; set; }

        public void Dispose()
        {
            HttpClient?.Dispose();
            MDNS?.Dispose();
            ServiceDiscovery?.Dispose();
        }

        public void StartService(CancellationToken cancellationToken = default)
        {
            Task.Run(async () =>
            {
                try
                {
                    MDNS.Start();
                    while (true)
                    {
                        SetupServiceDiscovery("_tempsensor._tcp.local");
                        while (string.IsNullOrEmpty(HostName)) await Task.Delay(1000, cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        while (true)
                        {
                            try
                            {
                                var jsonString = await HttpClient.GetStringAsync($"http://{HostName}/sensor");
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
                            catch (HttpRequestException e)
                            {
                                Logger.LogWarning(e, "Failed to get response; resetting hostname");
                                break;
                            }
                            catch (Exception e)
                            {
                                Logger.LogWarning(e, "Failed to fetch sensor data");
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

        public void SetupServiceDiscovery(string serviceName)
        {
            MDNS.SendQuery(serviceName, type: DnsType.PTR);

            ServiceDiscovery.ServiceInstanceDiscovered += (s, e) =>
            {
                MDNS.SendQuery(e.ServiceInstanceName, type: DnsType.SRV);
            };

            MDNS.AnswerReceived += (s, e) =>
            {
                var servers = e.Message.Answers.OfType<SRVRecord>();
                foreach (var server in servers) MDNS.SendQuery(server.Target, type: DnsType.A);
                var addresses = e.Message.Answers.OfType<AddressRecord>();
                foreach (var address in addresses)
                {
                    HostName = address.Address.ToString();
                    Logger.LogDebug($"Find service instance host: {HostName}");
                }
            };
        }
    }
}