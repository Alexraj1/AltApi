using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    public abstract class OneDriveItemBase
    {
        /// <summary>
        /// The original raw JSON message
        /// </summary>
        [JsonIgnore]
        public string OriginalJson { get; set; }
    }
}
