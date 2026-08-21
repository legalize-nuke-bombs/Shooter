using System;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Combat
{
    public struct BulletHole : INetworkSerializeByMemcpy, IEquatable<BulletHole>, ISaveable
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
        public void LoadObject(JToken content)
        {
            SaveData sd = content.To<SaveData>();
            Position = sd.Position;
            Normal = sd.Normal;
        }

        public bool Equals(BulletHole other)
        {
            return Position.Equals(other.Position) && Normal.Equals(other.Normal);
        }
    }
}
