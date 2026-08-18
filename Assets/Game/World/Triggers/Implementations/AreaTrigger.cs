using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(SphereCollider))]
    public class AreaTrigger : Trigger
    {
        private static readonly Journal Log = Logs.Here();

        private static int characterLayer = -1;

        private static int CharacterLayer =>
            characterLayer != -1 ? characterLayer : characterLayer = LayerMask.NameToLayer("Character");

        protected override void Awake()
        {
            base.Awake();

            SphereCollider sphere = GetComponent<SphereCollider>();
            if (!sphere.isTrigger) Log.Warn("Sphere must have a trigger!");
        }

        private void OnTriggerEnter(Collider target)
        {
            if (!IsServer) return;

            if (target.gameObject.layer != CharacterLayer) return;

            CharacterId characterId = target.GetComponentInParent<CharacterId>();
            if (characterId == null)
            {
                Log.Warn($"Character {target.name} does not have a persistent id");
                return;
            }

            OnTrigger(characterId);
        }
    }
}
