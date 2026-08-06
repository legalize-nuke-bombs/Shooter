using Shooter.Game.Body.Sounding;
using Shooter.Logging;
using Unity.Netcode.Components;
using UnityEngine;

namespace Shooter.Game.Falling
{
    [RequireComponent(typeof(StructureHealth))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public class Faller : MonoBehaviour, IBreakable
    {
        private static readonly Journal Log = Logs.Here();

        private StructureHealth structureHealth;
        private Speaker speaker;

        private Rigidbody rigidbody;
        private NetworkRigidbody networkRigidbody;
        [SerializeField] private float forceK = 0.001f;

        [SerializeField] private SoundSpec groundHitSound = null;
        [SerializeField] private float groundHitMinSpeed = 2f;
        [SerializeField] private float groundHitArmTilt = 30f;
        [SerializeField] private float maxDepenetrationSpeed = 1f;
        private bool waitingForGroundHit;
        private float initialTilt;
        private float fellAt;

        public void Awake()
        {
            structureHealth = GetComponent<StructureHealth>();
            speaker = GetComponent<Speaker>();

            rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = !structureHealth.Broken;
            rigidbody.maxDepenetrationVelocity = maxDepenetrationSpeed;
            waitingForGroundHit = structureHealth.Broken;
            initialTilt = Tilt();
            fellAt = Time.time;

            networkRigidbody = GetComponent<NetworkRigidbody>();
            networkRigidbody.AutoUpdateKinematicState = false;
        }

        private float Tilt()
        {
            return Vector3.Angle(transform.up, Vector3.up);
        }

        public void Broken()
        {
            Fall();
        }

        private void Fall()
        {
            Log.Info("Entity {} is falling!", name);

            rigidbody.isKinematic = false;
            waitingForGroundHit = true;
            initialTilt = Tilt();
            fellAt = Time.time;

            Vector3 pushDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            rigidbody.AddForceAtPosition(pushDirection * (forceK * rigidbody.mass), transform.position + Vector3.up * 3f, ForceMode.Impulse);
        }

        public void OnCollisionEnter(Collision collision)
        {
            GroundHit(collision);
        }

        public void OnCollisionStay(Collision collision)
        {
            GroundHit(collision);
        }

        private void GroundHit(Collision collision)
        {
            if (!waitingForGroundHit) return;

            float speedChange = collision.impulse.magnitude / rigidbody.mass;

            if (Tilt() - initialTilt < groundHitArmTilt) return;
            if (speedChange < groundHitMinSpeed) return;

            Log.Info("Entity {} hit the ground at {} m/s", name, speedChange);

            waitingForGroundHit = false;
            speaker.Play(groundHitSound);
        }
    }
}
