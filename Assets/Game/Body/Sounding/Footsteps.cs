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

        private readonly NetworkVariable<float> phase = new(0f, NetworkVariableReadPermission.Owner);

        private CharacterController body;
        private Movement movement;
        private Speaker speaker;
        private float stride;

        public float Phase => phase.Value;

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
            if (stride >= strideLength)
            {
                stride -= strideLength;
                speaker.Play(sounds == null ? null : sounds.On(Surface.Under(body)));
            }

            phase.Value = stride / strideLength;
        }
    }
}
