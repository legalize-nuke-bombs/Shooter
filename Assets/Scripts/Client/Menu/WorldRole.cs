using System.Runtime.Serialization;

namespace Shooter.Client.Menu
{
    public enum WorldRole
    {
        [EnumMember(Value = "MEMBER")]
        Member,

        [EnumMember(Value = "CREATOR")]
        Creator
    }
}
