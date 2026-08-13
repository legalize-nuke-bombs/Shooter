using System;
using UnityEngine;

namespace Shooter.Game.Combat
{
    public struct BulletHole : IEquatable<BulletHole>
    {
        public Vector3 Position;
        public Vector3 Normal;

        public bool Equals(BulletHole other)
        {
            return Position.Equals(other.Position) && Normal.Equals(other.Normal);
        }
    }
}
