using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Artifact : UniqueItem
    {
        public Artifact(string assetId) : base(assetId)
        {
        }

        private struct SaveData
        {

        }
        public override object SaveObject()
        {
            return new SaveData {  };
        }
        public override void LoadObject(SaveToken content)
        {

        }

        public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
        {

        }

        public void Update(GameObject user, float dt)
        {
            ArtifactAsset asset = Catalogs.GetCatalog<ItemCatalog>().GetAssetById(AssetId) as ArtifactAsset;
            List<ArtifactEffect> effects = asset.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                ArtifactEffect effect = effects.ElementAt(i);
                effect.Update(user, dt);
            }
        }
    }
}
