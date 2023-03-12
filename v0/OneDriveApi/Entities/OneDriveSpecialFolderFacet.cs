using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    public class OneDriveSpecialFolderFacet
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
