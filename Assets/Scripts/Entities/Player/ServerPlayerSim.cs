using UnityEngine;
using Shooter.Server;

namespace Shooter.Entities.Player
{
    public static class ServerPlayerSim
    {
        public static void SpawnBody(ServerPlayer player, float offsetX)
        {
            var body = new GameObject("Sim_" + player.UserId);

            float angle = (player.ConnId * 137f) % 360f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 16f;
            body.transform.position = new Vector3(offsetX + offset.x, 1.1f, offset.z);

            player.Body = body;
            player.Controller = body.AddComponent<CharacterController>();
            ServerLog.Info("spawned user " + player.UserId + " world " + player.WorldId + " at " + body.transform.position);
        }

        public static void Step(ServerPlayer p, float dt)
        {
            if (!p.InWorld || p.Controller == null) return;

            var input = new MotorInput
            {
                MoveX = p.LastInput.moveX,
                MoveZ = p.LastInput.moveZ,
                Sprint = p.LastInput.sprint,
                Jump = p.JumpQueued,
                Yaw = p.LastInput.yaw
            };
            float verticalVelocity = p.VerticalVelocity;
            PlayerMotor.Step(p.Controller, ref verticalVelocity, input, dt);
            p.VerticalVelocity = verticalVelocity;
            p.JumpQueued = false;
        }

        public static PlayerStateMsg BuildState(ServerPlayer p)
        {
            Vector3 pos = p.Body.transform.position;
            return new PlayerStateMsg
            {
                id = p.UserId,
                name = p.DisplayName,
                x = pos.x,
                y = pos.y,
                z = pos.z,
                yaw = p.Body.transform.eulerAngles.y,
                pitch = p.LastInput.pitch
            };
        }
    }
}
