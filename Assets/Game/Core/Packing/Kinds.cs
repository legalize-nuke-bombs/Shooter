using Unity.Netcode;

namespace Shooter.Game.Core
{
    public static class Kinds
    {
        public static void Use<TBase>(IKinds<TBase> kinds) where TBase : class, INetworkSerializable
        {
            Known<TBase>.Instance = kinds;
        }

        public static IKinds<TBase> Of<TBase>() where TBase : class, INetworkSerializable
        {
            return Known<TBase>.Instance ??= new TypeKinds<TBase>();
        }

        private static class Known<TBase> where TBase : class, INetworkSerializable
        {
            public static IKinds<TBase> Instance;
        }
    }
}
