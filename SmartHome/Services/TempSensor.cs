using System;
using System.Net.Sockets;
using System.Text;
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
        Task StartServiceAsync();
    }

    public class TempSensor : ITempSensor
    {
        public TempSensor(IConfiguration configuration, ILogger<TempSensor> logger,
            DbContextOptions<Database> dbOptions)
        {
            Configuration = configuration;
            Logger = logger;
            DbOptions = dbOptions;
            //StartService(CancellationTokenSource.Token).Start();
        }

        private IConfiguration Configuration { get; }
        private DbContextOptions<Database> DbOptions { get; }
        private ILogger<TempSensor> Logger { get; }
        private CancellationTokenSource CancellationTokenSource { get; set; }

        public async Task StartServiceAsync()
        {
            if (CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested)
                return;
            CancellationTokenSource?.Dispose();
            CancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = CancellationTokenSource.Token;

            await Task.Run(async () =>
            {
                try
                {
                    var tcpClient = new TcpClient {SendTimeout = 1000, ReceiveTimeout = 1000};
                    var buffer = new byte[256];
                    while (true)
                        try
                        {
                            if (!tcpClient.Connected)
                                if (!tcpClient.ConnectAsync(Configuration["TempSensor:Host"],
                                    int.Parse(Configuration["TempSensor:Port"])).Wait(1000))
                                    throw new TimeoutException();

                            var stream = tcpClient.GetStream();
                            var length = stream.ReadByte();
                            if (length != -1)
                            {
                                var readLength = 0;
                                while (length > readLength && !cancellationToken.IsCancellationRequested)
                                    readLength += await stream.ReadAsync(buffer, 0, length - readLength,
                                        cancellationToken);
                                var json = JObject.Parse(Encoding.UTF8.GetString(buffer, 0, length));
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

                            if (cancellationToken.IsCancellationRequested)
                                return;
                        }
                        catch (Exception e)
                        {
                            Logger.LogWarning(e, "Get sensor data failed");
                        }
                        finally
                        {
                            await Task.Delay(2000, cancellationToken);
                        }
                }
                finally
                {
                    CancellationTokenSource.Cancel();
                }
            }, cancellationToken);
        }
    }
}