using Shooter.Game.World;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(CharacterController))]
    public class Footsteps : NetworkBehaviour
    {
        [SerializeField] private float strideLength = 2f;

        [SerializeField] private SurfaceSounds sounds;

        private CharacterController body;
        private Movement movement;
        private Speaker speaker;
        private float stride;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
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
            if (!isActiveAndEnabled) return;

            stride += movement.GroundTravel;
            if (stride < strideLength) return;

            stride -= strideLength;
            speaker.Play(sounds == null ? null : sounds.On(Surface.Under(body)));
        }
    }
}
