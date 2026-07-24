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

        public static EntityView Resolve(Collider collider)
        {
            if (collider == null) return null;

            EntityBody link = collider.GetComponentInParent<EntityBody>();
            return link == null ? null : link.View;
        }
    }
}
