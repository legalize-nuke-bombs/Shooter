using UnityEngine;

namespace Shooter.Game.Core
{
    public class GameObjectId : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Id => id;

        public void Assign(string newId)
        {
            id = newId;
        }
    }
}
