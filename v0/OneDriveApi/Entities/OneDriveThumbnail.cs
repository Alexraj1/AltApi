using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    public class OneDriveThumbnail : OneDriveItemBase
    {
        [JsonPropertyName("width")]
        public long Width { get; set; }

        [JsonPropertyName("height")]
        public long Height { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
