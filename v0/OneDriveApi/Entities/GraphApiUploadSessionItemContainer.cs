using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    internal class GraphApiUploadSessionItemContainer : OneDriveItemBase
    {
        [JsonPropertyName("item")]
        public GraphApiUploadSessionItem Item { get; set; }
    }
}
