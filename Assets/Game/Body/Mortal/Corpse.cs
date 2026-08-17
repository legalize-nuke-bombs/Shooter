using Shooter.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;
using Shooter.Game.Core;

namespace Shooter.Game.Body
{
    public class Corpse : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<FixedString32Bytes> skin = new NetworkVariable<FixedString32Bytes>();
        private readonly NetworkVariable<FixedString32Bytes> title = new NetworkVariable<FixedString32Bytes>();

        private bool dressed;

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
            skin.OnValueChanged += Dressed;
            title.OnValueChanged += Titled;

            if (!skin.Value.IsEmpty) Dressed(default, skin.Value);
            if (!title.Value.IsEmpty) Titled(default, title.Value);
        }

        public override void OnNetworkDespawn()
        {
            skin.OnValueChanged -= Dressed;
            title.OnValueChanged -= Titled;
        }

        private void Dressed(FixedString32Bytes previous, FixedString32Bytes current)
        {
            if (dressed || current.IsEmpty) return;

            SkinCatalog catalog = Catalogs.Of<SkinCatalog>();
            SkinSpec spec = catalog == null ? null : catalog.Of(current);
            if (spec == null || spec.Model == null)
            {
                Log.Error($"Corpse {name} cannot find skin {current}, stays invisible");
                return;
            }

            dressed = true;

            GameObject body = Instantiate(spec.Model, transform);
            body.transform.localPosition = new Vector3(0f, -1f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.AddComponent<Ragdoll>();

            Log.Info($"Corpse {name} dressed as {current}");
        }

        private void Titled(FixedString32Bytes previous, FixedString32Bytes current)
        {
            var named = GetComponent<TypedNameable>();
            if (named == null || current.IsEmpty) return;

            NameCatalog catalog = Catalogs.Of<NameCatalog>();
            NameSpec spec = catalog == null ? null : catalog.Of(current);
            if (spec == null)
            {
                Log.Warn($"Corpse {name} cannot find name {current}, keeps its own");
                return;
            }

            named.Assign(spec);
        }
    }
}
