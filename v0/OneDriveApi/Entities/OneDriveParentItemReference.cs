using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    public class OneDriveParentItemReference : OneDriveItemBase
    {
        [JsonPropertyName("parentReference")]
        public OneDriveItemReference ParentReference { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
