using Shooter.Game.Body;
using Shooter.Game.Llm;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(SphereCollider))]
    public class Teleport : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject destination;

        [SerializeField] private EarSoundSpec sound = null;

        private static int characterLayer = -1;

        private static int CharacterLayer =>
            characterLayer != -1 ? characterLayer : characterLayer = LayerMask.NameToLayer("Character");

        private void Awake()
        {
            var sphere = GetComponent<SphereCollider>();
            if (!sphere.isTrigger) Log.Warn($"Teleport {name} has a solid sphere collider, tick Is Trigger for it to work");
        }

        private void OnTriggerEnter(Collider other)
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsServer) return;

            if (other.gameObject.layer != CharacterLayer) return;

            var movement = other.GetComponentInParent<Movement>();
            if (movement == null) return;

            Vector3 at = destination.transform.position;
            Log.Info($"Entity {movement.name} is teleported by {name} to {at}");

            movement.Teleport(at);
            movement.GetComponent<EarSpeaker>()?.Play(sound);
        }
    }
}
