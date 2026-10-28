using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    public class RespawnPosition : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Vector3? position;

        private struct SaveData
        {
            public Vector3? Position { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Position = position
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            position = saveData.Position;
        }

        private void Start()
        {
            if (position == null)
            {
                position = transform.position;
            }
        }

        public void SetPosition(Vector3 newPosition)
        {
            Log.Info($"Entity {name} got new respawn position {newPosition}");
            position = newPosition;
        }

        public Vector3 GetPosition()
        {
            if (position == null)
            {
                Log.Error($"Entity {name} does not have a respawn position yet, returning zero vector");
                return Vector3.zero;
            }
            return position.Value;
        }
    }
}
