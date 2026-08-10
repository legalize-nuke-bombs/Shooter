using Unity.Netcode;

namespace Shooter.Game.Core
{
    public interface IKinds<TBase> where TBase : class, INetworkSerializable
    {
        int Of(TBase value);

        TBase Create(int kind);
    }
}
