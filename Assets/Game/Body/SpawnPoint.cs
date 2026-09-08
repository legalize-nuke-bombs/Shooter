using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class SpawnPoint : MonoBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private Vector3? position;

        public string ComponentKey => "SpawnPoint";
        private struct SaveData
        {
            public Vector3? Position { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Position = position
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.To<SaveData>();
            position = sd.Position;
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
            Log.Info($"Entity {name} got new spawn point position {newPosition}");
            position = newPosition;
        }

        public Vector3 GetPosition()
        {
            if (position == null)
            {
                Log.Error($"Entity {name} does not have inited spawn point, returning zero vector");
                return Vector3.zero;
            }
            return position.Value;
        }
    }
}
