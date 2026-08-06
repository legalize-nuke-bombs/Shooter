using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Game.Body.EarSounding;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Teleport : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly Collider[] Inside = new Collider[64];

        [SerializeField] private float radius = 10f;
        [SerializeField] private GameObject destination;

        [SerializeField] private EarSoundSpec sound = null;

        [SerializeField] private float tickInterval = 1f;

        private readonly HashSet<Movement> moved = new HashSet<Movement>();

        private float sinceLastTick;

        private void Update()
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || !network.IsServer) return;

            sinceLastTick += Time.deltaTime;
            if (sinceLastTick < tickInterval) return;

            sinceLastTick -= tickInterval;
            Tick();
        }

        private void Tick()
        {
            int found = Physics.OverlapSphereNonAlloc(transform.position, radius, Inside);
            if (found == Inside.Length)
                Log.Warn("Teleport {} filled its buffer of {} colliders, someone in the radius may be left behind", name, Inside.Length);

            moved.Clear();

            for (int i = 0; i < found; i++)
            {
                var movement = Inside[i].GetComponentInParent<Movement>();
                if (movement == null || !moved.Add(movement)) continue;

                Vector3 at = destination.transform.position;
                Log.Info("Entity {} is teleported by {} to {}", movement.name, name, at);

                movement.Teleport(at);
                movement.GetComponent<EarSpeaker>()?.Play(sound);
            }
        }
    }
}
