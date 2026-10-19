using Shooter.Infrastructure;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Corpse : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly NetworkVariable<FixedString32Bytes> skinId = new();
        private readonly NetworkVariable<FixedString32Bytes> nameId = new();

        private bool dressed;

        public string ComponentKey => "Corpse";

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
        }

        public void AssignSkinAsset(SkinAsset skinAsset)
        {
            skinId.Value = skinAsset.NetworkId;
        }

        public void AssignNameAsset(NameAsset nameAsset)
        {
            nameId.Value = nameAsset.NetworkId;
        }

        public override void OnNetworkSpawn()
        {
            skinId.OnValueChanged += HandleSkinIdChanged;
            nameId.OnValueChanged += HandleNameIdChanged;

            if (!skinId.Value.IsEmpty) HandleSkinIdChanged(default, skinId.Value);
            if (!nameId.Value.IsEmpty) HandleNameIdChanged(default, nameId.Value);
        }

        public override void OnNetworkDespawn()
        {
            skinId.OnValueChanged -= HandleSkinIdChanged;
            nameId.OnValueChanged -= HandleNameIdChanged;
        }

        private void HandleSkinIdChanged(FixedString32Bytes previousSkinId, FixedString32Bytes currentSkinId)
        {
            if (dressed || currentSkinId.IsEmpty) return;

            SkinCatalog skinCatalog = Catalogs.GetCatalog<SkinCatalog>();
            SkinAsset skinAsset = skinCatalog == null ? null : skinCatalog.GetAssetById(currentSkinId);
            if (skinAsset == null || skinAsset.Model == null)
            {
                Log.Error($"Corpse {name} cannot find skin {currentSkinId}, stays invisible");
                return;
            }

            dressed = true;

            GameObject modelInstance = Instantiate(skinAsset.Model, transform);
            modelInstance.transform.localPosition = Skin.ModelOffset;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.AddComponent<Ragdoll>();

            Log.Info($"Corpse {name} dressed as {currentSkinId}");
        }

        private void HandleNameIdChanged(FixedString32Bytes previousNameId, FixedString32Bytes currentNameId)
        {
            TypedNameable typedNameable = GetComponent<TypedNameable>();
            if (typedNameable == null || currentNameId.IsEmpty) return;

            NameCatalog nameCatalog = Catalogs.GetCatalog<NameCatalog>();
            NameAsset nameAsset = nameCatalog == null ? null : nameCatalog.GetAssetById(currentNameId);
            if (nameAsset == null)
            {
                Log.Warn($"Corpse {name} cannot find name {currentNameId}, keeps its own");
                return;
            }

            typedNameable.AssignNameAsset(nameAsset);
        }
    }
}
