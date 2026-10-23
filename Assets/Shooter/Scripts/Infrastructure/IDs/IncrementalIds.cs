using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class IncrementalIds : ShooterBehaviour, ISaveableComponent
    {
        private long nextId;

        private struct SaveData
        {
            public long Next { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Next = nextId
            };
        }

        public void LoadObject(SaveToken content)
        {
            nextId = content.ToObject<SaveData>().Next;
        }

        public long TakeNextId()
        {
            return nextId++;
        }
    }
}
