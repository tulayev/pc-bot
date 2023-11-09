using Microsoft.Extensions.Configuration;

namespace PcTgBot.Settings
{
    internal static class Configuration<T>
    {
        private static readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        public static T GetSection(string section) =>
            _configuration.GetSection(section).Get<T>();
    }
}
