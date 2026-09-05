using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(MainRestrainable))]
    [RequireComponent(typeof(NetworkTransform))]
    public abstract class Movement : NetworkBehaviour
    {
        private const float PitchLimit = 89f;

        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 8f;

        private readonly NetworkVariable<float> pitch = new();
        private NetworkTransform networkTransform;
        private bool sprinting;
        private Vector2 steering;

        protected MainRestrainable Restrainable { get; private set; }

        public float Pitch => pitch.Value;

        public float GroundTravel { get; private set; }

        public float Yaw => transform.eulerAngles.y;

        public Vector3 Look => Quaternion.Euler(pitch.Value, Yaw, 0f) * Vector3.forward;

        protected virtual void Awake()
        {
            Restrainable = GetComponent<MainRestrainable>();
            networkTransform = GetComponent<NetworkTransform>();
        }

        public event Action<float> Turned;

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

        public void Steer(Vector2 move, float yaw, float look, bool sprint)
        {
            steering = Vector2.ClampMagnitude(Finite(move), 1f);
            transform.rotation = Quaternion.Euler(0f, Finite(yaw), 0f);
            pitch.Value = Mathf.Clamp(Finite(look), -PitchLimit, PitchLimit);

            sprinting = sprint;
        }

        public virtual void Halt()
        {
            steering = Vector2.zero;
            sprinting = false;
        }

        public void Teleport(Vector3 position)
        {
            Teleport(position, Yaw);
        }

        public void Teleport(Vector3 position, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0f, Finite(yaw), 0f);

            TeleportRaw(position, rotation);

            if (!IsServer) return;

            networkTransform.Teleport(position, rotation, transform.localScale);
            TurnRpc(Yaw);
        }

        protected abstract bool Tick(Vector3 wish, float dt);

        protected abstract void TeleportRaw(Vector3 position, Quaternion rotation);

        [Rpc(SendTo.Owner)]
        private void TurnRpc(float yaw)
        {
            Turned?.Invoke(yaw);
        }

        private void Step()
        {
            if (!isActiveAndEnabled) return;

            float dt = NetworkManager.LocalTime.FixedDeltaTime;

            bool walking = steering.SqrMagnitude() > 0f;
            sprinting = sprinting && walking;

            float speed;
            if (sprinting && Restrainable.CanPerform(ActionType.Sprint, dt))
            {
                Restrainable.RegisterAction(ActionType.Sprint, dt);
                speed = sprintSpeed;
            }
            else if (walking && Restrainable.CanPerform(ActionType.Walk, dt))
            {
                Restrainable.RegisterAction(ActionType.Walk, dt);
                speed = walkSpeed;
            }
            else
            {
                speed = 0;
            }

            Vector3 wish = transform.TransformDirection(new Vector3(steering.x, 0f, steering.y)) * speed;
            Vector3 before = transform.position;
            bool grounded = Tick(wish, dt);

            GroundTravel = grounded
                ? Vector3.Distance(new Vector3(before.x, 0f, before.z),
                    new Vector3(transform.position.x, 0f, transform.position.z))
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
