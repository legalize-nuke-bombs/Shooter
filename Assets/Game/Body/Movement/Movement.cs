using System;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Endurance))]
    public class Movement : NetworkBehaviour
    {
        public event Action<float> Turned;

        public event Action<float> Landed;

        private const float PitchLimit = 89f;
        private const float GroundedFall = -1f;

        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpSpeed = 5f;
        [SerializeField] private float gravity = -20f;

        private readonly NetworkVariable<float> pitch = new NetworkVariable<float>();

        private CharacterController characterController;
        private Endurance endurance;
        private IRestraint[] restraints;

        private Vector2 steering;
        private bool sprinting;
        private float fall;
        private bool jumping;
        private int steeredAt;

        public float Pitch => pitch.Value;

        public float GroundTravel { get; private set; }

        public float Yaw => transform.eulerAngles.y;

        public Vector3 Look => Quaternion.Euler(pitch.Value, Yaw, 0f) * Vector3.forward;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            endurance = GetComponent<Endurance>();
            restraints = GetComponents<IRestraint>();
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

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
        public void SteerRpc(Vector2 move, float yaw, float look, bool sprint, int tick)
        {
            if (tick <= steeredAt) return;
            steeredAt = tick;

            steering = Vector2.ClampMagnitude(Finite(move), 1f);
            transform.rotation = Quaternion.Euler(0f, Finite(yaw), 0f);
            pitch.Value = Mathf.Clamp(Finite(look), -PitchLimit, PitchLimit);

            sprinting = sprint;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void JumpRpc()
        {
            if (!characterController.isGrounded) return;

            jumping = true;
        }

        public void Halt()
        {
            steering = Vector2.zero;
            sprinting = false;
            jumping = false;
        }

        public void Teleport(Vector3 position)
        {
            Teleport(position, Yaw);
        }

        public void Teleport(Vector3 position, float yaw)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Finite(yaw), 0f));
            characterController.enabled = true;
            fall = 0f;

            if (IsServer) TurnRpc(Yaw);
        }

        [Rpc(SendTo.Owner)]
        private void TurnRpc(float yaw)
        {
            Turned?.Invoke(yaw);
        }

        private void Step()
        {
            float dt = NetworkManager.LocalTime.FixedDeltaTime;

            if (Restraints.Any(restraints)) Halt();

            if (characterController.isGrounded)
            {
                if (fall < GroundedFall) Landed?.Invoke(-fall);

                fall = jumping ? jumpSpeed : GroundedFall;
                jumping = false;
            }
            else
            {
                fall += gravity * dt;
            }

            bool moving = steering.sqrMagnitude > 0f;
            bool running = moving && sprinting && endurance.Sprint(dt);
            if (moving && !running) endurance.Walk(dt);

            float speed = running ? sprintSpeed : walkSpeed;
            Vector3 wish = transform.TransformDirection(new Vector3(steering.x, 0f, steering.y)) * speed;
            Vector3 before = transform.position;
            characterController.Move((wish + Vector3.up * fall) * dt);

            GroundTravel = characterController.isGrounded
                ? Vector3.Distance(new Vector3(before.x, 0f, before.z), new Vector3(transform.position.x, 0f, transform.position.z))
                : 0f;
        }

        private static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }

        private static Vector2 Finite(Vector2 value)
        {
            return new Vector2(Finite(value.x), Finite(value.y));
        }
    }
}
