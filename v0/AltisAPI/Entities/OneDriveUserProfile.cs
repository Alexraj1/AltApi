using AltApi.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;

namespace AltisAPI.Entities
{
    /// <summary>
    /// Represents an user
    /// </summary>
    public class OneDriveUserProfile : OneDriveItemBase
    {
        /// <summary>
        /// Unique identifier of the user
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Friendly name of the user
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// E-mail address of the user
        /// </summary>
        [JsonPropertyName("userPrincipalName")]
        public string userPrincipalName { get; set; }

        /// <summary>
        /// Username of the user
        /// </summary>
        [JsonPropertyName("givenName")]
        public string GivenName { get; set; }

        /// <summary>
        /// Username of the user
        /// </summary>
        [JsonPropertyName("surname")]
        public string SurName { get; set; }
        /// <summary>
        /// Organization the user belongs to
        /// </summary>
        [JsonPropertyName("organization")]
        public string Organization { get; set; }
    }
}
