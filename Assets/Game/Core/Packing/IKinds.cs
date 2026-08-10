using Unity.Netcode;

namespace Shooter.Game.Packing
{
    public interface IKinds<TBase> where TBase : class, INetworkSerializable
    {
        int Of(TBase value);

        TBase Create(int kind);
    }
}
