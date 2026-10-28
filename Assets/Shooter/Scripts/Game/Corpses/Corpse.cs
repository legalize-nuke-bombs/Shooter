using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Corpse : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<FixedString32Bytes> skinId = new();
        private readonly NetworkVariable<FixedString32Bytes> nameId = new();

        private bool dressed;

        private struct SaveData
        {
            public string Skin { get; set; }
            public string Title { get; set; }
        }

        public object SaveObject()
        {
            return new SaveData
            {
                Skin = skinId.Value.ToString(),
                Title = nameId.Value.ToString()
            };
        }

        public void LoadObject(SaveToken content)
        {
            SaveData saveData = content.ToObject<SaveData>();
            skinId.Value = saveData.Skin;
            nameId.Value = saveData.Title;
            Dress(skinId.Value);
            TakeName(nameId.Value);
        }

        public void AssignSkinAsset(SkinAsset skinAsset)
        {
            skinId.Value = skinAsset.NetworkId;
            Dress(skinId.Value);
        }

        public void AssignNameAsset(NameAsset nameAsset)
        {
            nameId.Value = nameAsset.NetworkId;
            TakeName(nameId.Value);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                skinId.OnValueChanged += HandleSkinIdReplicated;
                nameId.OnValueChanged += HandleNameIdReplicated;
            }

            Dress(skinId.Value);
            TakeName(nameId.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) return;

            skinId.OnValueChanged -= HandleSkinIdReplicated;
            nameId.OnValueChanged -= HandleNameIdReplicated;
        }

        private void HandleSkinIdReplicated(FixedString32Bytes previousSkinId, FixedString32Bytes currentSkinId)
        {
            Dress(currentSkinId);
        }

        private void HandleNameIdReplicated(FixedString32Bytes previousNameId, FixedString32Bytes currentNameId)
        {
            TakeName(currentNameId);
        }

        private void Dress(FixedString32Bytes skin)
        {
            if (dressed || skin.IsEmpty) return;

            SkinCatalog skinCatalog = Catalogs.GetCatalog<SkinCatalog>();
            SkinAsset skinAsset = skinCatalog == null ? null : skinCatalog.GetAssetById(skin);
            if (skinAsset == null || skinAsset.Model == null)
            {
                Log.Error($"Corpse {name} cannot find skin {skin}, stays invisible");
                return;
            }

            dressed = true;

            GameObject modelInstance = Instantiate(skinAsset.Model, transform);
            modelInstance.transform.localPosition = Skin.ModelOffset;
            modelInstance.transform.localRotation = Quaternion.identity;
            ShooterObject shooterObject = modelInstance.AddComponent<ShooterObject>();
            shooterObject.Placement = WorldPlacement.Movable;
            shooterObject.AddComponent<Ragdoll>();

            Log.Info($"Corpse {name} dressed as {skin}");
        }

        private void TakeName(FixedString32Bytes title)
        {
            AssetNameable assetNameable = GetComponent<AssetNameable>();
            if (assetNameable == null || title.IsEmpty) return;

            NameCatalog nameCatalog = Catalogs.GetCatalog<NameCatalog>();
            NameAsset nameAsset = nameCatalog == null ? null : nameCatalog.GetAssetById(title);
            if (nameAsset == null)
            {
                Log.Warn($"Corpse {name} cannot find name {title}, keeps its own");
                return;
            }

            assetNameable.AssignNameAsset(nameAsset);
        }
    }
}
