using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    public class OneDriveFolderFacet
    {
        [JsonPropertyName("childCount")]
        public long ChildCount { get; set; }
    }
}
