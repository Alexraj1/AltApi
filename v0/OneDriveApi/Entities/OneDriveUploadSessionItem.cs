using AltApi.Api.Enums;
using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    internal class OneDriveUploadSessionItem
    {
        [JsonPropertyName("@name.conflictBehavior")]
        public NameConflictBehavior FilenameConflictBehavior { get; set; }

        [JsonPropertyName("name")]
        public string Filename { get; set; }
    }
}
