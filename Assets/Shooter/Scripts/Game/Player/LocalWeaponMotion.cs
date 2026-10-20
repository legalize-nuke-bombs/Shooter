using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(LocalWeaponView))]
    [RequireComponent(typeof(Footsteps))]
    public class LocalWeaponMotion : NetworkBehaviour
    {
        private const float MinTeleportDistance = 1f;

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

        private Player player;
        private LocalWeaponView localWeaponView;
        private Footsteps footsteps;
        private bool tracking;

        private float phase;
        private bool otherFoot;
        private float walkingWeight;
        private float walkingWeightVelocity;

        private float lastYaw;
        private float lastPitch;
        private Vector2 lagAngles;
        private Vector2 lagAnglesVelocity;

        private Vector3 lastPosition;
        private float sidewaysSpeed;
        private float tiltAngle;
        private float tiltAngleVelocity;

        private void Awake()
        {
            player = GetComponent<Player>();
            localWeaponView = GetComponent<LocalWeaponView>();
            footsteps = GetComponent<Footsteps>();
        }

        private void OnEnable()
        {
            tracking = false;
        }

        public override void OnNetworkSpawn()
        {
            player.ControlChanged += HandleControlChanged;
            HandleControlChanged();
        }

        public override void OnNetworkDespawn()
        {
            player.ControlChanged -= HandleControlChanged;
        }

        private void HandleControlChanged()
        {
            enabled = player.IsControlledLocally;
        }

        private void LateUpdate()
        {
            Transform weaponHold = localWeaponView.WeaponHold;
            float dt = Time.deltaTime;
            if (weaponHold == null || dt <= 0f) return;

            Transform viewTransform = weaponHold.parent;
            if (!tracking) StartTracking(viewTransform);

            FollowFootstepPhase(dt);
            LagBehindLook(viewTransform, dt);
            TiltOnStrafe(dt);

            float footSide = otherFoot ? -1f : 1f;
            float drop = stepDrop * (0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * phase)) * walkingWeight;
            float sway = stepSway * Mathf.Sin(Mathf.PI * phase) * footSide * walkingWeight;

            weaponHold.localPosition = new Vector3(sway, -drop, 0f);
            weaponHold.localRotation = Quaternion.Euler(lagAngles.y, lagAngles.x, tiltAngle);
        }

        private void StartTracking(Transform viewTransform)
        {
            tracking = true;
            phase = footsteps.Phase;
            lastYaw = viewTransform.eulerAngles.y;
            lastPitch = viewTransform.eulerAngles.x;
            lastPosition = transform.position;
        }

        // The server sends the phase a tick at a time; here it runs smoothly, across a footfall too
        private void FollowFootstepPhase(float dt)
        {
            float phaseAhead = Mathf.Repeat(footsteps.Phase - phase + 0.5f, 1f) - 0.5f;
            float phaseStep = phaseAhead * (1f - Mathf.Exp(-dt / stepFollow));

            phase += phaseStep;
            if (phase >= 1f)
            {
                phase -= 1f;
                otherFoot = !otherFoot;
            }
            else if (phase < 0f)
            {
                phase += 1f;
            }

            float phaseSpeed = Mathf.Abs(phaseStep) / dt;
            walkingWeight = Mathf.SmoothDamp(walkingWeight, phaseSpeed > 0.05f ? 1f : 0f, ref walkingWeightVelocity, settle);
        }

        private void LagBehindLook(Transform viewTransform, float dt)
        {
            float yaw = viewTransform.eulerAngles.y;
            float pitch = viewTransform.eulerAngles.x;
            float yawSpeed = Mathf.DeltaAngle(lastYaw, yaw) / dt;
            float pitchSpeed = Mathf.DeltaAngle(lastPitch, pitch) / dt;
            lastYaw = yaw;
            lastPitch = pitch;

            Vector2 targetLagAngles = new(Mathf.Clamp(-yawSpeed * lookLag, -maxLag, maxLag), Mathf.Clamp(-pitchSpeed * lookLag, -maxLag, maxLag));
            lagAngles = Vector2.SmoothDamp(lagAngles, targetLagAngles, ref lagAnglesVelocity, settle);
        }

        private void TiltOnStrafe(float dt)
        {
            Vector3 position = transform.position;
            Vector3 shift = position - lastPosition;
            lastPosition = position;

            float strafeSpeed = shift.sqrMagnitude > MinTeleportDistance * MinTeleportDistance ? 0f : Vector3.Dot(shift, transform.right) / dt;

            sidewaysSpeed = Mathf.Lerp(sidewaysSpeed, strafeSpeed, 1f - Mathf.Exp(-dt / settle));
            float targetTiltAngle = Mathf.Clamp(-sidewaysSpeed * strafeTilt, -maxTilt, maxTilt);
            tiltAngle = Mathf.SmoothDamp(tiltAngle, targetTiltAngle, ref tiltAngleVelocity, settle);
        }
    }
}
