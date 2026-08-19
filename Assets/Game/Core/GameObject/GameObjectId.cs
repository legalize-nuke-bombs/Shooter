using UnityEngine;

namespace Shooter.Game.Core.GameObject
{
    public class GameObjectId : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Id => id;
    }
}
