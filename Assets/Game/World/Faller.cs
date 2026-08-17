using Shooter.Logging;
using Unity.Netcode.Components;
using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(StructureHealth))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public class Faller : MonoBehaviour, IBreakable
    {
        private static readonly Journal Log = Logs.Here();

        private StructureHealth structureHealth;

        private Rigidbody rigidbody;
        private NetworkRigidbody networkRigidbody;
        [SerializeField] private float forceK = 0.001f;

        [SerializeField] private float maxDepenetrationSpeed = 1f;

        private void Awake()
        {
            structureHealth = GetComponent<StructureHealth>();

            rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = !structureHealth.Broken;
            rigidbody.maxDepenetrationVelocity = maxDepenetrationSpeed;

            networkRigidbody = GetComponent<NetworkRigidbody>();
            networkRigidbody.AutoUpdateKinematicState = false;
        }

        public void Broken()
        {
            Fall();
        }

        private void Fall()
        {
            Log.Info($"Entity {this.NameOf()} is falling!");

            rigidbody.isKinematic = false;

            Vector3 pushDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            rigidbody.AddForceAtPosition(pushDirection * (forceK * rigidbody.mass), transform.position + Vector3.up * 3f, ForceMode.Impulse);
        }
    }
}
