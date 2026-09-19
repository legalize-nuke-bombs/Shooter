using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Landing))]
    public class RpcMovement : Movement
    {
        private const float GroundedFall = -1f;

        [SerializeField] private float jumpSpeed = 5f;
        [SerializeField] private float gravity = -20f;

        private bool airborne;
        private float airborneFrom;
        private CharacterController characterController;
        private float fall;
        private bool jumping;
        private Landing landing;
        private bool sprinting;
        private int steeredAt;
        private Vector2 steering;

        protected override void Awake()
        {
            base.Awake();

            characterController = GetComponent<CharacterController>();
            landing = GetComponent<Landing>();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
        public void SteerRpc(Vector2 move, float yaw, float look, bool sprint, int tick)
        {
            if (tick <= steeredAt) return;
            steeredAt = tick;

            steering = Vector2.ClampMagnitude(Finite(move), 1f);
            transform.rotation = Quaternion.Euler(0f, Finite(yaw), 0f);
            Pitch = look;
            sprinting = sprint;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void JumpRpc()
        {
            if (!characterController.isGrounded) return;

            jumping = true;
        }

        protected override float Tick(float dt)
        {
            bool walking = steering.sqrMagnitude > 0f;
            float speed = AffordSpeed(walking, sprinting, dt);
            Vector3 wish = transform.TransformDirection(new Vector3(steering.x, 0f, steering.y)) * speed;

            if (characterController.isGrounded)
            {
                if (airborne)
                {
                    airborne = false;
                    landing.Land(airborneFrom - transform.position.y);
                }

                if (jumping && Restrainable.CanPerform(ActionType.Jump, MainRestrainable.InstantAction))
                {
                    Restrainable.RegisterAction(ActionType.Jump, MainRestrainable.InstantAction);
                    fall = jumpSpeed;
                }
                else
                {
                    // Pressed down hard enough to stay on any slope the body can walk: with a weak press every step down a hill
                    // left the ground for a moment, footsteps thinned out and landings rattled instead
                    fall = Mathf.Min(GroundedFall, -speed * Mathf.Tan(characterController.slopeLimit * Mathf.Deg2Rad));
                }

                jumping = false;
            }
            else
            {
                if (!airborne)
                {
                    airborne = true;
                    airborneFrom = transform.position.y;

                    // The ground has really ended: the fall starts gently, as it did, not at the speed of the press. A jump keeps its own
                    if (fall < 0f) fall = GroundedFall;
                }
                else if (transform.position.y > airborneFrom)
                {
                    airborneFrom = transform.position.y;
                }

                fall += gravity * dt;
            }

            Vector3 before = transform.position;
            characterController.Move((wish + Vector3.up * fall) * dt);

            return characterController.isGrounded
                ? Vector2.Distance(new Vector2(before.x, before.z), new Vector2(transform.position.x, transform.position.z))
                : 0f;
        }

        protected override void TeleportRaw(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
            fall = 0f;
            airborne = false;
        }

        private static Vector2 Finite(Vector2 value)
        {
            return new Vector2(Finite(value.x), Finite(value.y));
        }
    }
}
