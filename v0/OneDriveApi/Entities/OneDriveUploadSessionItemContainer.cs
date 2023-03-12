using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    internal class OneDriveUploadSessionItemContainer : OneDriveItemBase
    {
        [JsonPropertyName("item")]
        public OneDriveUploadSessionItem Item { get; set; }
    }
}
