using System.Dynamic;
using Newtonsoft.Json;

namespace Stashie
{
    public class Settings
    {
        [JsonProperty("Ignored_Cells")]
        public int[,] IgnoredCells { get; set; }/* =
        {
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        };*/

        [JsonProperty("Hotkey")]
        public string Hotkey { get; set; } = "F3";
    }
}