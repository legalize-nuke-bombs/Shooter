using System.Runtime.Serialization;

namespace Shooter.Server.Sessions
{
    public enum SessionHookAction
    {
        Unknown,

        [EnumMember(Value = "OPEN_SESSION")]
        OpenSession,

        [EnumMember(Value = "CLOSE_SESSION")]
        CloseSession
    }
}
