using UnityEngine;

namespace Shooter.Infrastructure
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class GameObjectRuntimeIds : RegisteredBehaviour, ISaveableComponent
    {
        private long next;

        public string ComponentKey => "GameObjectRuntimeIds";
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
            SaveData sd = content.To<SaveData>();
            next = sd.Next;
        }

        public long Next()
        {
            return next++;
        }
    }
}
