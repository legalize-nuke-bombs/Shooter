using UnityEngine;

namespace Shooter.Client.Worlds.Entities
{
    public sealed class ClientEntityBody : MonoBehaviour
    {
        public EntityView View { get; private set; }

        public static void Attach(GameObject body, EntityView view)
        {
            body.AddComponent<ClientEntityBody>().View = view;
        }

        public static EntityView Resolve(Collider collider)
        {
            if (collider == null) return null;

            ClientEntityBody link = collider.GetComponentInParent<ClientEntityBody>();
            return link == null ? null : link.View;
        }
    }
}
