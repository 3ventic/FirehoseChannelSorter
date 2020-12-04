using Newtonsoft.Json;
using System.Collections.Generic;

namespace FirehoseChannelSorter
{
    public class Privmsg
    {
        [JsonIgnore] public Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        [JsonProperty("command")] public string Command { get; set; } = "I_NONE";
        [JsonProperty("room")] public string Room { get; set; } = "";
        [JsonIgnore] public string Channel => string.IsNullOrEmpty(Room) ? Target : Room;
        [JsonProperty("nick")] public string User { get; set; } = "";
        [JsonProperty("target")] public string Target { get; set; } = "";
        [JsonProperty("body")] public string Message { get; set; } = "";

        [JsonIgnore] private string TagStringInternal = "";
        [JsonProperty("tags")]
        public string TagString
        {
            get => TagStringInternal;
            set
            {
                string[] tagsParts = value.Split(';');
                foreach (string tag in tagsParts)
                {
                    int eqIndex = tag.IndexOf('=');
                    if (eqIndex == -1)
                    {
                        Tags[tag] = "true";
                    }
                    else
                    {
                        // Unescape tag value
                        Tags[tag.Substring(0, eqIndex)] = tag.Substring(eqIndex + 1)
                            .Replace(@"\:", ";")
                            .Replace(@"\s", " ")
                            .Replace(@"\\", @"\")
                            .Replace(@"\r", "\r")
                            .Replace(@"\n", "\n");
                    }
                }
                TagStringInternal = value;
            }
        }

        public override string ToString() => $"{TagStringInternal} {Command} {Channel} {User}{(string.IsNullOrEmpty(Target) ? string.Empty : $" > {Target}")}: {Message}";
    }
}
