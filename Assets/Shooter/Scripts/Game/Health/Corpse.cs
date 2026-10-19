using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Corpse : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly NetworkVariable<FixedString32Bytes> skin = new();
        private readonly NetworkVariable<FixedString32Bytes> title = new();

        private bool dressed;

        public string ComponentKey => "Corpse";
        private struct SaveData
        {
            public string Skin { get; set; }
            public string Title { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Skin = skin.Value.ToString(),
                Title = title.Value.ToString()
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            skin.Value = sd.Skin;
            title.Value = sd.Title;
        }

        public void Dress(SkinAsset asset)
        {
            skin.Value = asset.NetworkId;
        }

        public void Rename(NameAsset asset)
        {
            title.Value = asset.NetworkId;
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

            SkinCatalog catalog = Catalogs.GetCatalog<SkinCatalog>();
            SkinAsset asset = catalog == null ? null : catalog.GetAssetById(current);
            if (asset == null || asset.Model == null)
            {
                Log.Error($"Corpse {name} cannot find skin {current}, stays invisible");
                return;
            }

            dressed = true;

            GameObject body = Instantiate(asset.Model, transform);
            body.transform.localPosition = new Vector3(0f, -1f, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.AddComponent<Ragdoll>();

            Log.Info($"Corpse {name} dressed as {current}");
        }

        private void Titled(FixedString32Bytes previous, FixedString32Bytes current)
        {
            TypedNameable named = GetComponent<TypedNameable>();
            if (named == null || current.IsEmpty) return;

            NameCatalog catalog = Catalogs.GetCatalog<NameCatalog>();
            NameAsset asset = catalog == null ? null : catalog.GetAssetById(current);
            if (asset == null)
            {
                Log.Warn($"Corpse {name} cannot find name {current}, keeps its own");
                return;
            }

            named.AssignNameAsset(asset);
        }
    }
}
