using System.Runtime.Serialization;

namespace Shooter.Client.Menu
{
    public enum WorldJoinPolicy
    {
        [EnumMember(Value = "EVERYONE")]
        Everyone,

        [EnumMember(Value = "NOBODY")]
        Nobody
    }
}
