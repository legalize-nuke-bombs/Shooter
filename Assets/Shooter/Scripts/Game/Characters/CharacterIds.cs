using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class CharacterIds : ShooterBehaviour, ISaveableComponent
    {
        private long next;

        public string ComponentKey => "CharacterIds";

        private struct SaveData
        {
            public long Next { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Next = next
            };
        }

        public void LoadObject(SaveToken content)
        {
            next = content.ToObject<SaveData>().Next;
        }

        public long Next()
        {
            return next++;
        }
    }
}
