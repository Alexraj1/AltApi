﻿using System.Text.Json.Serialization;

namespace AltApi.Entities
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
