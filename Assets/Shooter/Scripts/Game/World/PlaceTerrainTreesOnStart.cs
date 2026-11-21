using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Terrain))]
    public class PlaceTerrainTreesOnStart : MonoBehaviour
    {
        private const string TreesName = "Trees";
        private static readonly Journal Log = Logs.Here();

        private void Start()
        {
            Terrain terrain = GetComponent<Terrain>();
            // For SpeedTree trees the terrain heeds no tree distance but zero, which stops it drawing them at all
            terrain.treeDistance = 0f;
            Transform trees = PlaceTrees(terrain);
            Log.Info($"{name} placed {trees.childCount} trees of its terrain as objects");
        }

        public static Transform PlaceTrees(Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            TreePrototype[] prototypes = terrainData.treePrototypes;
            Vector3 origin = terrain.transform.position;
            Transform trees = new GameObject(TreesName).transform;
            trees.SetParent(terrain.transform, false);

            foreach (TreeInstance tree in terrainData.treeInstances)
            {
                Vector3 position = origin + Vector3.Scale(tree.position, terrainData.size);
                Quaternion rotation = Quaternion.Euler(0f, tree.rotation * Mathf.Rad2Deg, 0f);
                Transform placed = Instantiate(prototypes[tree.prototypeIndex].prefab, position, rotation, trees).transform;
                placed.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
            }

            return trees;
        }
    }
}
