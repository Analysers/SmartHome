using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace SmartHome.Services
{
    public interface ITelegramBot
    {
        void StartService(CancellationToken cancellationToken = default);
    }

    public class TelegramBot : ITelegramBot
    {
        public TelegramBot(IConfiguration configuration, ILogger<TelegramBot> logger,
            DbContextOptions<Database> dbOptions)
        {
            Configuration = configuration;
            Logger = logger;
            DbOptions = dbOptions;
            Bot = new TelegramBotClient(configuration["Telegram:BotKey"]);
            logger.LogInformation("Telegram bot enabled");
        }

        private IConfiguration Configuration { get; }
        private DbContextOptions<Database> DbOptions { get; }
        private ILogger<TelegramBot> Logger { get; }
        private TelegramBotClient Bot { get; }

        public void StartService(CancellationToken cancellationToken = default)
        {
            Bot.OnMessage += async (sender, args) =>
            {
                if (Regex.IsMatch(args.Message.Chat.Id.ToString(), Configuration["Telegram:ChatId"]))
                {
                    var cmdArgs = args.Message.Text.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    if (cmdArgs.Length == 0)
                        return;
                    switch (cmdArgs[0])
                    {
                        case "/temp":
                        case "/temp@aritionBot":
                        case "/temperature":
                        case "/temperature@aritionBot":
                            using (var database = new Database(DbOptions))
                            {
                                var temperature =
                                    await database.Temperature.OrderByDescending(t => t.Time).FirstAsync(cancellationToken);
                                var sendString = $"Temperature: {temperature.TemperatureCelsius}\n" +
                                                 $"Last update: {temperature.Time.ToLocalTime():g}";
                                await Bot.SendTextMessageAsync(args.Message.Chat.Id, sendString,
                                    cancellationToken: cancellationToken);
                            }

                            break;
                        case "/humidity":
                        case "/humidity@aritionBot":
                            using (var database = new Database(DbOptions))
                            {
                                var humidity = await database.Humidity.OrderByDescending(t => t.Time).FirstAsync(cancellationToken);
                                var sendString = $"Humidity: {humidity.HumidityPercent}\n" +
                                                 $"Last update: {humidity.Time.ToLocalTime():g}";
                                await Bot.SendTextMessageAsync(args.Message.Chat.Id, sendString,
                                    cancellationToken: cancellationToken);
                            }

                            break;
                        default:
                            Logger.LogWarning($"Unknown command received: {args.Message.Text}");
                            break;
                    }
                }
            };

            Bot.StartReceiving(cancellationToken: cancellationToken);
        }
    }
}