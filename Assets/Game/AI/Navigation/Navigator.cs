using Shooter.Game.Body;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game.AI.Navigation
{
    [RequireComponent(typeof(Movement))]
    public class Navigator : NetworkBehaviour
    {
        public enum State
        {
            Idle,
            Walking,
            Arrived,
            Unreachable
        }

        private const float CornerReach = 0.35f;
        private const float SampleReach = 2f;
        private const float StrayLimit = 2.5f;
        private const float TurnSpeed = 360f;
        private const int MaxCorners = 64;

        private static readonly Journal Log = Logs.Here();

        private readonly Vector3[] corners = new Vector3[MaxCorners];
        private int cornerCount;
        private Vector3 destination;

        private Movement movement;
        private int nextCorner;
        private NavMeshPath path;
        private bool sprinting;

        public State Progress { get; private set; } = State.Idle;

        private void Awake()
        {
            movement = GetComponent<Movement>();
            path = new NavMeshPath();
        }

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

        public void Walk(Vector3 target, bool sprint = false)
        {
            sprinting = sprint;

            if (!TryPlot(target))
            {
                Log.Info($"Entity {name} found no path to {target}");
                Progress = State.Unreachable;
                movement.Halt();
                return;
            }

            Log.Info($"Entity {name} walks to {destination} over {cornerCount} corners");
            Progress = State.Walking;
        }

        public void Stop()
        {
            Progress = State.Idle;
            movement.Halt();
        }

        private bool TryPlot(Vector3 target)
        {
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit from, SampleReach, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(target, out NavMeshHit to, SampleReach, NavMesh.AllAreas)) return false;
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;

            cornerCount = path.GetCornersNonAlloc(corners);
            if (cornerCount == 0) return false;

            destination = to.position;
            nextCorner = 0;
            return true;
        }

        private void Step()
        {
            if (!isActiveAndEnabled) return;
            if (Progress != State.Walking) return;

            Vector3 position = transform.position;
            while (nextCorner < cornerCount && Flat(corners[nextCorner] - position).magnitude < CornerReach) nextCorner++;

            if (nextCorner >= cornerCount)
            {
                Log.Info($"Entity {name} arrived at {destination}");
                Progress = State.Arrived;
                movement.Halt();
                return;
            }

            if (Strayed(position))
            {
                Log.Info($"Entity {name} strayed off its path, replotting");
                Walk(destination, sprinting);
                return;
            }

            Vector3 toCorner = Flat(corners[nextCorner] - position);
            float wantedYaw = Mathf.Atan2(toCorner.x, toCorner.z) * Mathf.Rad2Deg;
            float dt = NetworkManager.LocalTime.FixedDeltaTime;
            float yaw = Mathf.MoveTowardsAngle(movement.Yaw, wantedYaw, TurnSpeed * dt);

            movement.Steer(Vector2.up, yaw, 0f, sprinting);
        }

        private bool Strayed(Vector3 position)
        {
            Vector3 from = nextCorner > 0 ? corners[nextCorner - 1] : corners[0];
            Vector3 to = corners[nextCorner];

            Vector3 segment = Flat(to - from);
            Vector3 reach = Flat(position - from);

            float length = segment.magnitude;
            if (length < CornerReach) return reach.magnitude > StrayLimit;

            float along = Mathf.Clamp(Vector3.Dot(reach, segment / length), 0f, length);
            return (reach - segment / length * along).magnitude > StrayLimit;
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }
    }
}
