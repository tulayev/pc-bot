using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace PcTgBot.Commands
{
    internal class Screenshot
    {
        private Bitmap GetPrintScreen()
        {
            var screen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            var graphics = Graphics.FromImage(screen);
            graphics.CopyFromScreen(0, 0, 0, 0, screen.Size);

            return screen;
        }

        public async Task SendPrintScreen(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream())
            {
                var screenshot = GetPrintScreen();
                screenshot.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                await botClient.SendPhotoAsync(
                    chatId: update.Message.Chat.Id,
                    photo: InputFile.FromStream(ms),
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
