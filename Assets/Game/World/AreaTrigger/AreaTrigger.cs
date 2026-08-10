using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(SphereCollider))]
    public abstract class AreaTrigger : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static int characterLayer = -1;
        private static int CharacterLayer =>
            characterLayer != -1 ? characterLayer : characterLayer = LayerMask.NameToLayer("Character");

        private readonly HashSet<long> done = new HashSet<long>();
        [SerializeField] private bool allowReiteration = true;

        private void Awake()
        {
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (!sphere.isTrigger) Log.Warn("Sphere must has a trigger!");
        }

        private void OnTriggerEnter(Collider target)
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsServer) return;

            if (target.gameObject.layer != CharacterLayer) return;

            PersistentId persistentId = target.GetComponentInParent<PersistentId>();
            if (persistentId == null)
            {
                Log.Warn($"Character {persistentId.name} does not have a persistent id");
                return;
            }

            if (!allowReiteration)
            {
                if (!done.Add(persistentId.Value))
                {
                    return;
                }
            }

            Log.Info($"Entity {name} is going to trigger on {persistentId.name}");
            OnTrigger(persistentId);
        }

        protected abstract void OnTrigger(PersistentId character);
    }
}
