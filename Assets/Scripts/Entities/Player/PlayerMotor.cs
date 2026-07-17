using UnityEngine;

namespace Shooter.Entities.Player
{
    public struct MotorInput
    {
        public float MoveX;
        public float MoveZ;
        public bool Sprint;
        public bool Jump;
        public float Yaw;
    }

    public static class PlayerMotor
    {
        public const float WalkSpeed = 5f;
        public const float SprintSpeed = 8f;
        public const float JumpHeight = 1.2f;
        public const float Gravity = -20f;

        public static void Step(CharacterController controller, ref float verticalVelocity, in MotorInput input, float dt)
        {
            Transform body = controller.transform;
            body.rotation = Quaternion.Euler(0f, input.Yaw, 0f);

            Vector3 direction = Vector3.ClampMagnitude(body.right * input.MoveX + body.forward * input.MoveZ, 1f);
            float speed = input.Sprint ? SprintSpeed : WalkSpeed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (input.Jump)
                    verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }

            verticalVelocity += Gravity * dt;
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * dt);
        }
    }
}
