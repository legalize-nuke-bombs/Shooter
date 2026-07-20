using System;
using UnityEngine;

namespace Shooter.Server.Worlds.Entities
{
    public sealed class EntityBody : MonoBehaviour
    {
        public Guid Id { get; private set; }

        public static void Bind(GameObject body, Guid id)
        {
            body.AddComponent<EntityBody>().Id = id;
        }

        public static bool TryResolve(Collider collider, out Guid id)
        {
            EntityBody link = collider.GetComponentInParent<EntityBody>();
            id = link == null ? Guid.Empty : link.Id;
            return link != null;
        }
    }
}
