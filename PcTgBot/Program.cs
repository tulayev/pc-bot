using System.Threading;
using System;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using System.Threading.Tasks;
using NLog;

namespace PcTgBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            NLogConfiguration.Setup();
            var botClient = new TelegramBotClient(ConfigSettings.Creds.BotToken);
            var respond = new Respond();

            using (var cts = new CancellationTokenSource())
            {
                // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
                };

                botClient.StartReceiving(
                    updateHandler: respond.HandleUpdateAsync,
                    pollingErrorHandler: respond.HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );

                var me = await botClient.GetMeAsync();

                LogManager.GetCurrentClassLogger().Info($"Start listening for @{me.Username}");
                Console.WriteLine($"Start listening for @{me.Username}");
                Console.ReadLine();

                // Flush and close down internal threads and timers
                LogManager.Shutdown();
                // Send cancellation request to stop bot
                cts.Cancel();
            }
        }
    }
}
