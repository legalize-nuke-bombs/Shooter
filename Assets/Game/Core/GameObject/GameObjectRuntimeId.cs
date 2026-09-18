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

            // Not in Awake: a network variable written before the spawn does not know its behaviour yet
            GameObjectRuntimeIds ids = GameState.Get<GameObjectRuntimeIds>();
            value.Value = ids == null ? UnityEngine.Random.Range(0, int.MaxValue) : ids.Next();
        }

        public static GameObjectRuntimeId Of(long id, Inactive gate)
        {
            foreach (GameObjectRuntimeId component in Registers.Of<GameObjectRuntimeId>(gate))
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
