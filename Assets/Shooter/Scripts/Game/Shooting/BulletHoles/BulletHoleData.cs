using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public struct BulletHoleData : INetworkSerializeByMemcpy, ISaveableObject
    {
        public Vector3 Position;
        public Vector3 Normal;

        private struct SaveData
        {
            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Position = Position,
                Normal = Normal
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            Position = saveData.Position;
            Normal = saveData.Normal;
        }
    }
}
