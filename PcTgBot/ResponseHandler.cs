using System.Threading.Tasks;
using System.Threading;
using System;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot;
using PcTgBot.Commands;
using NLog;

namespace PcTgBot
{
    internal class ResponseHandler
    {
        private readonly Screenshot _screenshot;
        private readonly PCSystem _system;
        private readonly PCProcess _process;

        public ResponseHandler()
        {
            _screenshot = new Screenshot();
            _system = new PCSystem();
            _process = new PCProcess(_system);
        }

        private async Task<string> GetResponse(ITelegramBotClient botClient, Update update, string text, CancellationToken cancellationToken)
        {
            var response = _process.ManageBotState(text);

            switch (text)
            {
                case Constants.ScreenshotCommand:
                    await _screenshot.SendPrintScreen(botClient, update, cancellationToken);
                    response = "Screenshot sent";
                    break;
                case Constants.ProcessesCommand: 
                    response = _process.GetRunningProcessesList(); 
                    break;
                case Constants.StartProcessCommand: 
                    response = "Which one to start?"; 
                    _process.BotState = BotState.StartProcess; 
                    break;
                case Constants.KillProcessCommand: 
                    response = $"{_process.GetRunningProcessesList()}\r\nWhich one to kill?";
                    _process.BotState = BotState.KillProcess; 
                    break;
                case Constants.AppsCommand: 
                    response = _system.GetInstalledAppsList(); 
                    break;
                case Constants.SysInfoCommand: 
                    response = _system.GetSystemInfo(); 
                    break;
                case Constants.SearchWebCommand: 
                    response = "Google it! Type something...";
                    _process.BotState = BotState.SearchWeb; 
                    break;
                case Constants.SendMessageCommand: 
                    response = "Write something...";
                    _process.BotState = BotState.SendMessage; 
                    break;
                case Constants.ShutdownCommand:
                    response = "WARNING! Your PC is going to be turned off! Do you want to proceed next?\r\nType Yes (y) or No (n)";
                    _process.BotState = BotState.Shutdown; 
                    break;
            }

            return response;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;
            var messageText = message.Text;

            if (!(message is Message) || !(messageText is string))
                return;

            var chatId = message.Chat.Id;

            // Restrict access to bot usage, provide in appsettings.json your own user id.
            if (chatId != ConfigSettings.Creds.UserId)
                return;

            LogManager.GetCurrentClassLogger().Info($"Received a '{messageText}' message in chat {chatId}.");

            var response = await GetResponse(botClient, update, messageText, cancellationToken);
            response = string.IsNullOrWhiteSpace(response) ? $"Unknown command {messageText}" : response;

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: response,
                replyMarkup: CommandsKeyboard.Instance.GetReplyKeyboardMarkup(),
                cancellationToken: cancellationToken
            );
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            string errorMessage;

            switch (exception)
            {
                case ApiRequestException apiRequestException:
                    errorMessage = $"Telegram API Error:\r\n[{apiRequestException.ErrorCode}]\r\n{apiRequestException.Message}";
                    break;
                default:
                    errorMessage = exception.ToString();
                    break;
            };

            LogManager.GetCurrentClassLogger().Error(errorMessage);

            return Task.CompletedTask;
        }
    }
}
