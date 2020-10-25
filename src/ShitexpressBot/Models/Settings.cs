using System.Collections.Generic;

namespace ShitexpressBot.Models
{
    public class Settings
    {
        public BotSettings Bot { get; set; }

        public Dictionary<string, string> Animals { get; set; }

        public Dictionary<string, string> Stickers { get; set; }

        public Dictionary<string, string> Countries { get; set; }
    }

    public class BotSettings
    {
        public string Token { get; set; }

        public string Username { get; set; }
    }
}
