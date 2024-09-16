using Newtonsoft.Json;
using System;

namespace AwqatSalaat.Services.GitHub
{
    public class Release
    {
        [JsonProperty("tag_name")]
        public string Tag { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("draft")]
        public bool IsDraft { get; set; }

        [JsonProperty("prerelease")]
        public bool IsPreRelease { get; set; }

        public Version GetVersion()
        {
            if (string.IsNullOrEmpty(Tag))
            {
                throw new InvalidOperationException("Tag is missing.");
            }

            var v = new Version(Tag.Replace("v", "").Replace("V", ""));

            return new Version(
                v.Major,
                v.Minor,
                v.Build == -1 ? 0 : v.Build,
                v.Revision == -1  ? 0 : v.Revision);
        }
    }
}
