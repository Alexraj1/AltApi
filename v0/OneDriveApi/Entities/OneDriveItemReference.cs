﻿using System.Text.Json.Serialization;

namespace AltApi.Api.Entities
{
    public class OneDriveItemReference : OneDriveItemBase
    {
        [JsonPropertyName("driveId")]
        public string DriveId { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }
    }
}
