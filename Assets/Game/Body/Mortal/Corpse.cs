using Shooter.Game.Body.Hitboxes;
using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Corpse : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<FixedString32Bytes> skin = new NetworkVariable<FixedString32Bytes>();
        private readonly NetworkVariable<NameableType> title = new NetworkVariable<NameableType>(NameableType.DeadPlayer);

        public void Dress(SkinSpec spec)
        {
            skin.Value = spec.Id;
        }

        public void Rename(NameableType type)
        {
            title.Value = type;
        }

        public override void OnNetworkSpawn()
        {
            GetComponent<TypedNameable>()?.Assign(title.Value);

            if (skin.Value.IsEmpty)
            {
                Log.Warn("Corpse {} has no skin id, stays invisible", name);
                return;
            }

            SkinCatalog catalog = Environment.Current == null ? null : Environment.Current.Skins;
            SkinSpec spec = catalog == null ? null : catalog.Of(skin.Value);
            if (spec == null || spec.Model == null)
            {
                Log.Error("Corpse {} cannot find skin {}, stays invisible", name, skin.Value);
                return;
            }

            GameObject body = Instantiate(spec.Model, transform);
            body.transform.localPosition = new Vector3(0f, -1f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.AddComponent<Ragdoll>();

            Log.Info("Corpse {} dressed as {}", name, skin.Value);
        }
    }
}
