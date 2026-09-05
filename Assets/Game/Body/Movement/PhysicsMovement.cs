using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Landing))]
    public class PhysicsMovement : Movement
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
        private int steeredAt;

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

            Steer(move, yaw, look, sprint);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void JumpRpc()
        {
            if (!characterController.isGrounded) return;

            jumping = true;
        }

        public override void Halt()
        {
            base.Halt();
            jumping = false;
        }

        protected override bool Advance(Vector3 wish, float dt)
        {
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
                    fall = GroundedFall;
                }

                jumping = false;
            }
            else
            {
                if (!airborne)
                {
                    airborne = true;
                    airborneFrom = transform.position.y;
                }
                else if (transform.position.y > airborneFrom)
                {
                    airborneFrom = transform.position.y;
                }

                fall += gravity * dt;
            }

            characterController.Move((wish + Vector3.up * fall) * dt);
            return characterController.isGrounded;
        }

        protected override void Relocate(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
            fall = 0f;
        }
    }
}
