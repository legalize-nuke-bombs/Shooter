using System;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using Unity.Netcode;

namespace Shooter.Game.Core
{
    public class GameObjectRuntimeId : RegisteredNetworkBehaviour, ISaveableComponent, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        public const long Default = -1;

        // The server hands the id out, so every client reads the same number as the server
        private readonly NetworkVariable<long> value = new(Default);

        public long Value => value.Value;

        public string ComponentKey => "GameObjectRuntimeId";

        public DigestionPriority Priority => DigestionPriority.Handle;

        private struct SaveData
        {
            public long Id { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Id = Value
            };
        }

        public void LoadObject(SaveToken content)
        {
            value.Value = content.To<SaveData>().Id;
        }

        public string Digest(DigestionDetail detail)
        {
            return "[ID " + Value + "]";
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer || value.Value != Default) return;

            // Handed out at the spawn, not in Awake: the variable knows its behaviour by now, and the value
            // still rides in the spawn message itself; a save loaded afterwards overrides it
            GameObjectRuntimeIds ids = GameObjectRuntimeIds.Current;
            value.Value = ids == null ? UnityEngine.Random.Range(0, int.MaxValue) : ids.Next();
        }

        public static GameObjectRuntimeId Of(long id, Inactive gate)
        {
            foreach (GameObjectRuntimeId component in Registers.Current.Of<GameObjectRuntimeId>(gate))
            {
                if (component.Value == id)
                {
                    return component;
                }

            }
            return null;
        }
    }
}
