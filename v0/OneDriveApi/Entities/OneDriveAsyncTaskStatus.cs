using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    public class OneDriveAsyncTaskStatus : OneDriveItemBase
    {
        [JsonPropertyName("operation")]
        public Enums.OneDriveAsyncJobType Operation { get; set; }

        [JsonPropertyName("percentageComplete")]
        public double PercentComplete { get; set; }

        [JsonPropertyName("status")]
        public Enums.OneDriveAsyncJobStatus Status { get; set; }
        
    }
}
