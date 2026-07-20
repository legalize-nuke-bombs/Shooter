using UnityEngine;

namespace Shooter.Server.Worlds.Entities
{
    public sealed class EntityBody : MonoBehaviour
    {
        public long Id { get; private set; }

        public static void Bind(GameObject body, long id)
        {
            body.AddComponent<EntityBody>().Id = id;
        }

        public static bool TryResolve(Collider collider, out long id)
        {
            EntityBody link = collider.GetComponentInParent<EntityBody>();
            id = link == null ? 0L : link.Id;
            return link != null;
        }
    }
}
