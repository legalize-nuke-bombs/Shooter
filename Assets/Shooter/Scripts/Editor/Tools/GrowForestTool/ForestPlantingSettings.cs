using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Editor
{
    [Serializable]
    public class ForestPlantingSettings
    {
        [Serializable]
        public class Species
        {
            public GameObject tree;
            public float share = 1f;
            public Vector2 scaleRange = new(0.8f, 1.2f);
        }

        [Serializable]
        public class LayerDensity
        {
            public TerrainLayer layer;
            [Range(0f, 1f)] public float density = 1f;
        }

        public List<Species> species = new();
        public List<LayerDensity> layerDensities = new();
        public float spacing = 8f;
        public float groveSize = 60f;
        [Range(0f, 1f)] public float patchiness = 0.5f;
        public float steepestSlope = 40f;
        public float treelineHeight = 350f;
        public float distanceFromWater = 4f;
        public float clearanceAroundObjects = 2f;
        public LayerMask obstacles = Physics.DefaultRaycastLayers;
    }
}
