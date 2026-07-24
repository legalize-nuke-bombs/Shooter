using System;
using UnityEngine;

namespace Shooter.Server.Worlds.Entities
{
    public sealed class ServerEntityBody : MonoBehaviour
    {
        public Guid Id { get; private set; }

        public static void Bind(GameObject body, Guid id)
        {
            body.AddComponent<ServerEntityBody>().Id = id;
        }

        public static bool TryResolve(Collider collider, out Guid id)
        {
            ServerEntityBody link = collider.GetComponentInParent<ServerEntityBody>();
            id = link == null ? Guid.Empty : link.Id;
            return link != null;
        }
    }
}
