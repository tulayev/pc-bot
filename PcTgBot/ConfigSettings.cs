using PcTgBot.Settings;
using System.IO;

namespace PcTgBot
{
    internal static class ConfigSettings
    {
        public static Creds Creds =>
            Configuration<Creds>.GetSection(nameof(Creds));

        public static string ProjectFolder =>
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
    }
}
