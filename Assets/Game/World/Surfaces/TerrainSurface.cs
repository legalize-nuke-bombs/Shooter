using System;
using UnityEngine;

namespace Shooter.Game.World
{
    [Serializable]
    public struct TerrainSurface
    {
        [SerializeField] private TerrainLayer layer;
        [SerializeField] private PhysicsMaterial surface;

        public TerrainLayer Layer => layer;
        public PhysicsMaterial Surface => surface;
    }
}
