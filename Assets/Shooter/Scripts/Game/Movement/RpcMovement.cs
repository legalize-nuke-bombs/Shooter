using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Landing))]
    public class RpcMovement : Movement
    {
        private const float GroundedVerticalSpeed = -1f;

        [SerializeField] private float jumpSpeed = 5f;
        [SerializeField] private float gravity = -20f;

        private bool airborne;
        private float fallStartHeight;
        private CharacterController characterController;
        private float verticalSpeed;
        private bool jumpRequested;
        private Landing landing;
        private bool sprinting;
        private int lastSteerTick;
        private Vector2 steeringInput;

        protected override void ShooterAwake()
        {
            base.ShooterAwake();

            characterController = GetComponent<CharacterController>();
            landing = GetComponent<Landing>();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
        public void SteerRpc(Vector2 moveInput, float yaw, float pitch, bool sprint, int tick)
        {
            if (tick <= lastSteerTick) return;
            lastSteerTick = tick;

            steeringInput = Vector2.ClampMagnitude(ZeroIfNotFinite(moveInput), 1f);
            transform.rotation = Quaternion.Euler(0f, ZeroIfNotFinite(yaw), 0f);
            Pitch = pitch;
            sprinting = sprint;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void JumpRpc()
        {
            if (!characterController.isGrounded) return;

            jumpRequested = true;
        }

        protected override float MoveForTick(float dt)
        {
            bool walking = steeringInput.sqrMagnitude > 0f;
            float speed = AffordSpeed(walking, sprinting, dt);
            Vector3 horizontalVelocity = transform.TransformDirection(new Vector3(steeringInput.x, 0f, steeringInput.y)) * speed;

            if (characterController.isGrounded)
            {
                if (airborne)
                {
                    airborne = false;
                    landing.HandleLanding(fallStartHeight - transform.position.y);
                }

                if (jumpRequested && Hands.TryPerform(ActionType.Jump, 0f))
                {
                    verticalSpeed = jumpSpeed;
                }
                else
                {
                    // Pressed down hard enough to stay on any walkable slope: with a weak press every step down a hill
                    // left the ground for a moment, footsteps thinned out and landings rattled
                    verticalSpeed = Mathf.Min(GroundedVerticalSpeed, -speed * Mathf.Tan(characterController.slopeLimit * Mathf.Deg2Rad));
                }

                jumpRequested = false;
            }
            else
            {
                if (!airborne)
                {
                    airborne = true;
                    fallStartHeight = transform.position.y;

                    // The ground has really ended: the fall starts gently, not at the speed of the press; a jump keeps its own speed
                    if (verticalSpeed < 0f) verticalSpeed = GroundedVerticalSpeed;
                }
                else if (transform.position.y > fallStartHeight)
                {
                    fallStartHeight = transform.position.y;
                }

                verticalSpeed += gravity * dt;
            }

            Vector3 positionBefore = transform.position;
            characterController.Move((horizontalVelocity + Vector3.up * verticalSpeed) * dt);

            if (!characterController.isGrounded) return 0f;

            return Vector2.Distance(new Vector2(positionBefore.x, positionBefore.z), new Vector2(transform.position.x, transform.position.z));
        }

        protected override void TeleportLocally(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
            verticalSpeed = 0f;
            airborne = false;
        }

        private static Vector2 ZeroIfNotFinite(Vector2 value)
        {
            return new Vector2(ZeroIfNotFinite(value.x), ZeroIfNotFinite(value.y));
        }
    }
}
