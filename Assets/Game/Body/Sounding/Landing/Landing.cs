using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body.Sounding
{
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(Speaker))]
    public class Landing : NetworkBehaviour
    {
        [SerializeField] private float minSpeed = 3f;

        [SerializeField] private SoundSpec sound;

        private Movement movement;
        private Speaker speaker;

        private void Awake()
        {
            movement = GetComponent<Movement>();
            speaker = GetComponent<Speaker>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            movement.Landed += Landed;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            movement.Landed -= Landed;
        }

        private void Landed(float speed)
        {
            if (speed < minSpeed) return;

            speaker.Play(sound);
        }
    }
}
