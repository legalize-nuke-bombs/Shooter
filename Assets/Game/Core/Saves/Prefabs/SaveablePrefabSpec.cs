using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    [CreateAssetMenu(menuName = "Shooter-saves/Saveable Prefab", fileName = "SaveablePrefab")]
    public class SaveablePrefabSpec : Spec
    {
        [SerializeField] private UnityEngine.GameObject prefab;

        public UnityEngine.GameObject Prefab => prefab;
    }
}
