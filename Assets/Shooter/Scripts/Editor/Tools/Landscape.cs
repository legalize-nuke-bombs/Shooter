using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editor
{
    public static class Landscape
    {
        private const string Root = "Landscape";

        public static bool Selected()
        {
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected.GetComponent<Terrain>() != null) return true;
            }

            return false;
        }

        public static List<Terrain> All()
        {
            GameObject root = GameObject.Find(Root);
            return root != null ? new List<Terrain>(root.GetComponentsInChildren<Terrain>()) : new List<Terrain>(Terrain.activeTerrains);
        }

        public static List<Terrain> Targets()
        {
            var chosen = new List<Terrain>();
            foreach (GameObject selected in Selection.gameObjects)
            {
                Terrain terrain = selected.GetComponent<Terrain>();
                if (terrain != null && !chosen.Contains(terrain))
                {
                    chosen.Add(terrain);
                }
            }

            return chosen.Count > 0 ? chosen : All();
        }
    }
}
