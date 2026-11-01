using UnityEngine;

namespace Shooter.Infrastructure
{
    [CreateAssetMenu(menuName = "Shooter/Saves/Saveable Prefab", fileName = "SaveablePrefab")]
    public class SaveablePrefabAsset : Asset
    {
        [SerializeField] private GameObject prefab;

        public GameObject Prefab => prefab;
    }
}
