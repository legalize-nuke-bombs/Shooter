using UnityEngine;

namespace Shooter.Client.Worlds.Entities
{
    public sealed class EntityBody : MonoBehaviour
    {
        public EntityView View { get; private set; }

        public static void Attach(GameObject body, EntityView view)
        {
            body.AddComponent<EntityBody>().View = view;
        }
    }
}
