using Newtonsoft.Json;

namespace Stashie
{
    public class Settings
    {
        [JsonProperty("Ignored_Cells")]
        public int[,] IgnoredCells { get; set; }

        [JsonProperty("Hotkey")]
        public string Hotkey { get; set; }
    }
}