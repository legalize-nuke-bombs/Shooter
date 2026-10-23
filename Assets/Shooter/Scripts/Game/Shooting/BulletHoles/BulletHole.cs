using System;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public struct BulletHole : INetworkSerializeByMemcpy, IEquatable<BulletHole>, ISaveableObject
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

        public bool Equals(BulletHole other)
        {
            return Position.Equals(other.Position) && Normal.Equals(other.Normal);
        }
    }
}
