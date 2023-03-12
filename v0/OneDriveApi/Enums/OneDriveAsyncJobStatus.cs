using System.Runtime.Serialization;

namespace AltApi.Api.Enums
{
    public enum OneDriveAsyncJobStatus
    {
        [EnumMember(Value = "NotStarted")]
        NotStarted,

        [EnumMember(Value = "InProgress")]
        InProgress,

        [EnumMember(Value = "Complete")]
        Complete
    }
}
