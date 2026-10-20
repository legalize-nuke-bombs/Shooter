using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(MainTriggerable))]
    [RequireComponent(typeof(SphereCollider))]
    public class AreaTrigger : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private MainTriggerable mainTriggerable;

        private Character ownCharacter;

        private static int characterLayer = -1;
        private static int CharacterLayer => characterLayer != -1 ? characterLayer : characterLayer = LayerMask.NameToLayer("Character");

        protected override void ShooterAwake()
        {
            mainTriggerable = GetComponent<MainTriggerable>();
            ownCharacter = GetComponentInParent<Character>();

            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (!sphereCollider.isTrigger) Log.Error($"Entity {name} has a sphere collider that is not a trigger");
        }

        private void OnTriggerEnter(Collider target)
        {
            if (!IsServer) return;

            if (target.gameObject.layer != CharacterLayer) return;

            Character character = target.GetComponentInParent<Character>();
            if (character == null)
            {
                Log.Warn($"Body {target.name} has no Character component");
                return;
            }

            if (character == ownCharacter) return;

            mainTriggerable.HandleCharacterEntered(character);
        }
    }
}
