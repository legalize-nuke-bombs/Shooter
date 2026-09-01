using System;
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
        private const float CornerReach = 0.35f;
        private const float SampleReach = 2f;
        private const float StrayLimit = 2.5f;
        private const float TurnSpeed = 360f;
        private const int MaxCorners = 64;

        private static readonly Journal Log = Logs.Here();
        
        private Movement movement;
        private NavMeshPath path;

        private readonly Vector3[] corners = new Vector3[MaxCorners];
        private int cornerCount;
        private int nextCorner;

        public NavigatorStatus Status { get; private set; } = NavigatorStatus.Idle;
        public string TaskName { get; private set; }
        public bool Sprinting { get; private set; }
        public Vector3 Destination { get; private set; }

        public struct CallbackData
        {
            public NavigatorStatus Status { get; set; }
            public string TaskName { get; set; }
            public bool Sprinting { get; set; }
            public Vector3 Destination { get; set; }
            public string InterrupterName { get; set; }
        }

        private Action<CallbackData> onFinished;
        private bool finishing;

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

        public void GoTo(string taskName, bool sprint, Action<CallbackData> onFinish, Vector3 target)
        {
            if (finishing)
            {
                Log.Error($"Entity {name} rejects task {taskName}: GoTo called from a finish callback");
                return;
            }

            Interrupt(taskName);

            TaskName = taskName;
            Sprinting = sprint;
            onFinished = onFinish;

            Replot(target);
        }

        public void Interrupt(string interrupterName)
        {
            if (Status != NavigatorStatus.Walking) return;

            Log.Info($"Entity {name} interrupted task {TaskName} by {interrupterName}");
            Status = NavigatorStatus.Interrupted;
            movement.Halt();
            Finish(Snapshot(NavigatorStatus.Interrupted, Destination, interrupterName));
        }

        private void Replot(Vector3 target)
        {
            if (!TryPlot(target))
            {
                Log.Info($"Entity {name} found no path to {target}");
                Status = NavigatorStatus.Unreachable;
                movement.Halt();
                Finish(Snapshot(NavigatorStatus.Unreachable, target));
                return;
            }

            Log.Info($"Entity {name} going to {Destination} over {cornerCount} corners");
            Status = NavigatorStatus.Walking;
        }

        private void Finish(CallbackData data)
        {
            Action<CallbackData> callback = onFinished;
            if (callback == null) return;

            finishing = true;

            try
            {
                callback.Invoke(data);
            }
            catch (Exception exception)
            {
                Log.Error($"Entity {name} finish callback of task {data.TaskName} failed: {exception}");
            }

            finishing = false;
        }

        private CallbackData Snapshot(NavigatorStatus status, Vector3 destination, string interrupterName = null)
        {
            return new CallbackData
            {
                Status = status,
                TaskName = TaskName,
                Sprinting = Sprinting,
                Destination = destination,
                InterrupterName = interrupterName
            };
        }

        private bool TryPlot(Vector3 target)
        {
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit from, SampleReach, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(target, out NavMeshHit to, SampleReach, NavMesh.AllAreas)) return false;
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;

            cornerCount = path.GetCornersNonAlloc(corners);
            if (cornerCount == 0) return false;

            Destination = to.position;
            nextCorner = 0;
            return true;
        }

        private void Step()
        {
            if (!isActiveAndEnabled) return;
            if (Status != NavigatorStatus.Walking) return;

            Vector3 position = transform.position;
            while (nextCorner < cornerCount && Flat(corners[nextCorner] - position).magnitude < CornerReach) nextCorner++;

            if (nextCorner >= cornerCount)
            {
                Log.Info($"Entity {name} arrived at {Destination}");
                Status = NavigatorStatus.Arrived;
                movement.Halt();
                Finish(Snapshot(NavigatorStatus.Arrived, Destination));
                return;
            }

            if (Strayed(position))
            {
                Log.Info($"Entity {name} strayed off its path, replotting");
                Replot(Destination);
                return;
            }

            Vector3 toCorner = Flat(corners[nextCorner] - position);
            float wantedYaw = Mathf.Atan2(toCorner.x, toCorner.z) * Mathf.Rad2Deg;
            float dt = NetworkManager.LocalTime.FixedDeltaTime;
            float yaw = Mathf.MoveTowardsAngle(movement.Yaw, wantedYaw, TurnSpeed * dt);

            movement.Steer(Vector2.up, yaw, 0f, Sprinting);
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
