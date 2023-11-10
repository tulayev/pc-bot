using System;
using Telegram.Bot.Types.ReplyMarkups;

namespace PcTgBot.Commands
{
    internal class CommandsKeyboard
    {
        private readonly ReplyKeyboardMarkup _keyboardMarkup;
        private static readonly Lazy<CommandsKeyboard> _instatnce = new Lazy<CommandsKeyboard>(() => new CommandsKeyboard());

        public static CommandsKeyboard Instance => _instatnce.Value;

        private CommandsKeyboard()
        {
            _keyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { Constants.ScreenshotCommand, Constants.ProcessesCommand, Constants.StartProcessCommand },
                new KeyboardButton[] { Constants.KillProcessCommand, Constants.AppsCommand, Constants.SysInfoCommand },
                new KeyboardButton[] { Constants.SearchWebCommand, Constants.SendMessageCommand, Constants.ShutdownCommand }
            })
            {
                ResizeKeyboard = true
            };
        }

        public ReplyKeyboardMarkup GetReplyKeyboardMarkup()
        {
            return _keyboardMarkup;
        }
    }
}
