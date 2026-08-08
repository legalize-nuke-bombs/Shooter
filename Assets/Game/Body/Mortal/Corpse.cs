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
        private readonly NetworkVariable<FixedString32Bytes> title = new NetworkVariable<FixedString32Bytes>();

        public void Dress(SkinSpec spec)
        {
            skin.Value = spec.Id;
        }

        public void Rename(NameSpec spec)
        {
            title.Value = spec.Id;
        }

        public override void OnNetworkSpawn()
        {
            Inherit();

            if (skin.Value.IsEmpty)
            {
                Log.Warn($"Corpse {name} has no skin id, stays invisible");
                return;
            }

            SkinCatalog catalog = Environment.Current == null ? null : Environment.Current.Skins;
            SkinSpec spec = catalog == null ? null : catalog.Of(skin.Value);
            if (spec == null || spec.Model == null)
            {
                Log.Error($"Corpse {name} cannot find skin {skin.Value}, stays invisible");
                return;
            }

            GameObject body = Instantiate(spec.Model, transform);
            body.transform.localPosition = new Vector3(0f, -1f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.AddComponent<Ragdoll>();

            Log.Info($"Corpse {name} dressed as {skin.Value}");
        }

        private void Inherit()
        {
            var named = GetComponent<TypedNameable>();
            if (named == null || title.Value.IsEmpty) return;

            NameCatalog catalog = Environment.Current == null ? null : Environment.Current.Names;
            NameSpec spec = catalog == null ? null : catalog.Of(title.Value);
            if (spec == null)
            {
                Log.Warn($"Corpse {name} cannot find name {title.Value}, keeps its own");
                return;
            }

            named.Assign(spec);
        }
    }
}
