using System.Runtime.Serialization;

namespace AltApi.Api.Enums
{
    public enum OneDriveAsyncJobType
    {
        [EnumMember(Value = "DownloadUrl")]
        DownloadUrl,
        [EnumMember(Value = "CopyItem")]
        CopyItem
    }
}
