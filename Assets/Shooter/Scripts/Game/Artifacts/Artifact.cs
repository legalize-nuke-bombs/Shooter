using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Artifact : UniqueItem
    {
        private readonly List<ArtifactEffect> effects;

        public Artifact(string assetId, List<ArtifactEffect> effects) : base(assetId)
        {
            this.effects = effects;
        }

        public void Enable(GameObject self)
        {
            foreach (ArtifactEffect effect in effects)
            {
                effect.Enable(self);
            }
        }

        public void Disable()
        {
            foreach (ArtifactEffect effect in effects )
            {
                effect.Disable();
            }
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

        public void Update(float dt)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                ArtifactEffect effect = effects.ElementAt(i);
                effect.Update(dt);
            }
        }
    }
}
