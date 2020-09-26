using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
namespace HappyHourParser
{
    class Config
    {
        private static object locker = new object();
        private static Config _config { get; set; }
        public static Config GetConfig()
        {
            if (_config != null)
                return _config;
            _config = null;
            if (File.Exists("config.json"))
                using (StreamReader r = new StreamReader("config.json"))
                    _config = JsonConvert.DeserializeObject<Config>(r.ReadToEnd());
            if (_config.DiscordWebhookURLs == null)
                _config.DiscordWebhookURLs = new List<string>();
            if (_config.TelegramUserIDs == null)
                _config.TelegramUserIDs = new List<int>();
            return _config ?? new Config();
        }
        public string TelegramBotToken { get; set; }
        public List<int> TelegramUserIDs { get; set; }
        public List<string> DiscordWebhookURLs { get; set; }
        private Config() { }
        public static void Save()
        {
            lock (locker)
            {
                using (StreamWriter w = new StreamWriter("config.json"))
                    w.WriteLine(JsonConvert.SerializeObject(_config));
            }
        }
    }
}
