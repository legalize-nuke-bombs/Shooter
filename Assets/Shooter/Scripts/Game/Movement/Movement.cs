using System;
using Shooter.Infrastructure;
using Unity.Netcode.Components;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(MainRestrainable))]
    [RequireComponent(typeof(NetworkTransform))]
    public abstract class Movement : ShooterNetworkBehaviour
    {
        private const float PitchLimit = 89f;

        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 8f;

        private readonly NetworkVariable<float> pitch = new();
        private NetworkTransform networkTransform;

        protected MainRestrainable Restrainable { get; private set; }

        public float Pitch
        {
            get => pitch.Value;
            protected set => pitch.Value = Mathf.Clamp(ZeroIfNotFinite(value), -PitchLimit, PitchLimit);
        }

        public float GroundTravel { get; private set; }

        public float Yaw => transform.eulerAngles.y;

        public Vector3 LookDirection => Quaternion.Euler(pitch.Value, Yaw, 0f) * Vector3.forward;

        protected override void ShooterAwake()
        {
            Restrainable = GetComponent<MainRestrainable>();
            networkTransform = GetComponent<NetworkTransform>();
        }

        public event Action<float> Turned;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick += HandleNetworkTick;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= HandleNetworkTick;
        }

        public void Teleport(Vector3 position)
        {
            Teleport(position, Yaw);
        }

        public void Teleport(Vector3 position, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0f, ZeroIfNotFinite(yaw), 0f);

            TeleportLocally(position, rotation);

            if (!IsServer) return;

            networkTransform.Teleport(position, rotation, transform.localScale);
            TurnRpc(Yaw);
        }

        protected abstract float MoveForTick(float dt);

        protected abstract void TeleportLocally(Vector3 position, Quaternion rotation);

        protected float AffordSpeed(bool walking, bool sprinting, float dt)
        {
            if (!walking) return 0f;

            if (sprinting && Restrainable.CanPerform(ActionType.Sprint, dt))
            {
                Restrainable.RegisterAction(ActionType.Sprint, dt);
                return sprintSpeed;
            }

            if (Restrainable.CanPerform(ActionType.Walk, dt))
            {
                Restrainable.RegisterAction(ActionType.Walk, dt);
                return walkSpeed;
            }

            return 0f;
        }

        protected static float ZeroIfNotFinite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }

        [Rpc(SendTo.Owner)]
        private void TurnRpc(float yaw)
        {
            Turned?.Invoke(yaw);
        }

        private void HandleNetworkTick()
        {
            if (!isActiveAndEnabled) return;

            GroundTravel = MoveForTick(NetworkManager.LocalTime.FixedDeltaTime);
        }
    }
}
