using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(Speaker))]
    [RequireComponent(typeof(CharacterController))]
    public class Footsteps : NetworkBehaviour
    {
        [SerializeField] private float strideLength = 2f;

        [SerializeField] private SurfaceSounds sounds;

        private readonly NetworkVariable<float> phase = new(0f, NetworkVariableReadPermission.Owner);

        private CharacterController characterController;
        private Movement movement;
        private Speaker speaker;
        private float distanceSinceLastStep;

        public float Phase => phase.Value;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            movement = GetComponent<Movement>();
            speaker = GetComponent<Speaker>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick += AdvanceStride;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= AdvanceStride;
        }

        private void AdvanceStride()
        {
            if (!isActiveAndEnabled) return;

            distanceSinceLastStep += movement.GroundTravel;
            if (distanceSinceLastStep >= strideLength)
            {
                distanceSinceLastStep -= strideLength;
                speaker.PlayForEveryone(sounds == null ? null : sounds.GetSoundOn(Surface.FindUnder(characterController)));
            }

            phase.Value = distanceSinceLastStep / strideLength;
        }
    }
}
