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

        protected MainRestrainable Restrainable { get; private set; }

        public float Pitch
        {
            get => pitch.Value;
            protected set => pitch.Value = Mathf.Clamp(Finite(value), -PitchLimit, PitchLimit);
        }

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

        protected abstract float Tick(float dt);

        protected abstract void TeleportRaw(Vector3 position, Quaternion rotation);

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

        protected static float Finite(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }

        [Rpc(SendTo.Owner)]
        private void TurnRpc(float yaw)
        {
            Turned?.Invoke(yaw);
        }

        private void Step()
        {
            if (!isActiveAndEnabled) return;

            GroundTravel = Tick(NetworkManager.LocalTime.FixedDeltaTime);
        }
    }
}
