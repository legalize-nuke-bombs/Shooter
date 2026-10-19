using UnityEngine;

namespace Shooter.Infrastructure
{
    [CreateAssetMenu(menuName = "Shooter-saves/Saveable Prefab", fileName = "SaveablePrefab")]
    public class SaveablePrefabAsset : Asset
    {
        [SerializeField] private GameObject prefab;

        public GameObject Prefab => prefab;
    }
}
