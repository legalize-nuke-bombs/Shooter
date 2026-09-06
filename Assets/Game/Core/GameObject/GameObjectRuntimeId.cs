using System;
using Shooter.Game.Core.Saves;
using Shooter.Logging;

namespace Shooter.Game.Core
{
    public class GameObjectRuntimeId : RegisteredBehaviour, ISaveableComponent, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        public const long Default = -1;

        public long Value { get; private set; } = Default;

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
            Value = content.To<SaveData>().Id;
        }

        public string Digest(DigestionDetail detail)
        {
            return "[ID " + Value + "]";
        }

        protected override void Awake()
        {
            base.Awake();
            GameObjectRuntimeIds ids = GameObjectRuntimeIds.Current;
            if (ids == null)
            {
                Value = UnityEngine.Random.Range(0, int.MaxValue);
            }
            else
            {
                Value = GameObjectRuntimeIds.Current.Next();
            }
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
