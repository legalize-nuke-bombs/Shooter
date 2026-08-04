using Shooter.Game.Body.Sounding;
using Shooter.Logging;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Shooter.Game.Falling
{
    [RequireComponent(typeof(StructureHealth))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public class Faller : NetworkBehaviour, IBreakable
    {
        private static readonly Journal Log = Logs.Here();

        private StructureHealth structureHealth;
        private Speaker speaker;
        [SerializeField] private SoundSpec fallingSound = null;

        private Rigidbody rigidbody;
        private NetworkRigidbody networkRigidbody;
        [SerializeField] private float forceK = 0.1f;

        public void Awake()
        {
            structureHealth = GetComponent<StructureHealth>();
            speaker = GetComponent<Speaker>();

            rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = !structureHealth.Broken;

            networkRigidbody = GetComponent<NetworkRigidbody>();
            networkRigidbody.AutoUpdateKinematicState = false;
        }

        public void Broken()
        {
            Fall();
        }

        private void Fall()
        {
            Log.Info("Entity {} is falling!", name);

            speaker.Play(fallingSound);

            rigidbody.isKinematic = false;

            Vector3 pushDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            rigidbody.AddForceAtPosition(pushDirection * (forceK * rigidbody.mass), transform.position + Vector3.up * 3f, ForceMode.Impulse);
        }
    }
}
