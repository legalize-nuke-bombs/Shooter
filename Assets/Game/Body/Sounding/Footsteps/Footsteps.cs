using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body.Sounding
{
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(Speaker))]
    public class Footsteps : NetworkBehaviour
    {
        [SerializeField] private float strideLength = 2f;

        [SerializeField] private SoundSpec sound;

        private Movement movement;
        private Speaker speaker;
        private float stride;

        private void Awake()
        {
            movement = GetComponent<Movement>();
            speaker = GetComponent<Speaker>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick += Step;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= Step;
        }

        private void Step()
        {
            stride += movement.GroundTravel;
            if (stride < strideLength) return;

            stride -= strideLength;
            speaker.Play(sound);
        }
    }
}
