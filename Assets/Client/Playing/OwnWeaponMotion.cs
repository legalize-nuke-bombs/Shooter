using Shooter.Game.Body;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(OwnWeapon))]
    [RequireComponent(typeof(Footsteps))]
    public class OwnWeaponMotion : NetworkBehaviour
    {
        private const float TeleportJump = 1f;

        [Header("Steps")]
        [SerializeField] private float stepDrop = 0.012f;
        [SerializeField] private float stepSway = 0.008f;
        [SerializeField] private float stepFollow = 0.05f;

        [Header("Look")]
        [SerializeField] private float lookLag = 0.015f;
        [SerializeField] private float maxLag = 2.5f;

        [Header("Strafe")]
        [SerializeField] private float strafeTilt = 1.5f;
        [SerializeField] private float maxTilt = 4f;

        [SerializeField] private float settle = 0.12f;

        private OwnWeapon weapon;
        private Footsteps footsteps;
        private bool primed;

        private float phase;
        private bool otherFoot;
        private float walking;
        private float walkingVelocity;

        private float lastYaw;
        private float lastPitch;
        private Vector2 lag;
        private Vector2 lagVelocity;

        private Vector3 lastPosition;
        private float sideways;
        private float tilt;
        private float tiltVelocity;

        private void Awake()
        {
            weapon = GetComponent<OwnWeapon>();
            footsteps = GetComponent<Footsteps>();
        }

        private void OnEnable()
        {
            primed = false;
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;
        }

        public override void OnGainedOwnership()
        {
            enabled = IsOwner;
        }

        public override void OnLostOwnership()
        {
            enabled = false;
        }

        private void LateUpdate()
        {
            Transform hold = weapon.Hold;
            float dt = Time.deltaTime;
            if (hold == null || dt <= 0f) return;

            Transform eye = hold.parent;
            if (!primed) Prime(eye);

            Step(dt);
            Look(eye, dt);
            Strafe(dt);

            float side = otherFoot ? -1f : 1f;
            float drop = stepDrop * (0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * phase)) * walking;
            float sway = stepSway * Mathf.Sin(Mathf.PI * phase) * side * walking;

            hold.localPosition = new Vector3(sway, -drop, 0f);
            hold.localRotation = Quaternion.Euler(lag.y, lag.x, tilt);
        }

        private void Prime(Transform eye)
        {
            primed = true;
            phase = footsteps.Phase;
            lastYaw = eye.eulerAngles.y;
            lastPitch = eye.eulerAngles.x;
            lastPosition = transform.position;
        }

        // The phase comes from the server a tick at a time and runs smoothly here, across a footfall too
        private void Step(float dt)
        {
            float ahead = Mathf.Repeat(footsteps.Phase - phase + 0.5f, 1f) - 0.5f;
            float moved = ahead * (1f - Mathf.Exp(-dt / stepFollow));

            phase += moved;
            if (phase >= 1f)
            {
                phase -= 1f;
                otherFoot = !otherFoot;
            }
            else if (phase < 0f)
            {
                phase += 1f;
            }

            float pace = Mathf.Abs(moved) / dt;
            walking = Mathf.SmoothDamp(walking, pace > 0.05f ? 1f : 0f, ref walkingVelocity, settle);
        }

        private void Look(Transform eye, float dt)
        {
            float yaw = eye.eulerAngles.y;
            float pitch = eye.eulerAngles.x;
            float yawSpeed = Mathf.DeltaAngle(lastYaw, yaw) / dt;
            float pitchSpeed = Mathf.DeltaAngle(lastPitch, pitch) / dt;
            lastYaw = yaw;
            lastPitch = pitch;

            var behind = new Vector2(
                Mathf.Clamp(-yawSpeed * lookLag, -maxLag, maxLag),
                Mathf.Clamp(-pitchSpeed * lookLag, -maxLag, maxLag));
            lag = Vector2.SmoothDamp(lag, behind, ref lagVelocity, settle);
        }

        private void Strafe(float dt)
        {
            Vector3 position = transform.position;
            Vector3 shift = position - lastPosition;
            lastPosition = position;

            // A teleport is not a step to the side
            float speed = shift.sqrMagnitude > TeleportJump * TeleportJump ? 0f : Vector3.Dot(shift, transform.right) / dt;

            sideways = Mathf.Lerp(sideways, speed, 1f - Mathf.Exp(-dt / settle));
            float lean = Mathf.Clamp(-sideways * strafeTilt, -maxTilt, maxTilt);
            tilt = Mathf.SmoothDamp(tilt, lean, ref tiltVelocity, settle);
        }
    }
}
