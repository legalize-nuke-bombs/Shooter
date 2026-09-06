using System;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentMovement : Movement
    {
        private const float SampleReach = 2f;

        private static readonly Journal Log = Logs.Here();

        private NavMeshAgent agent;
        private NavMeshPath scratch;
        private Vector3 written;

        public AgentMovementStatus Status { get; private set; } = AgentMovementStatus.Idle;
        public string TaskName { get; private set; }
        public bool Sprinting { get; private set; }
        public Vector3 Destination { get; private set; }

        public struct CallbackData
        {
            public AgentMovementStatus Status { get; set; }
            public string TaskName { get; set; }
            public bool Sprinting { get; set; }
            public Vector3 Destination { get; set; }
            public string InterrupterName { get; set; }
        }

        private Action<CallbackData> onFinished;
        private bool finishing;

        protected override void Awake()
        {
            base.Awake();

            agent = GetComponent<NavMeshAgent>();
            agent.updatePosition = false;
            agent.updateRotation = false;
            scratch = new NavMeshPath();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            agent.enabled = IsServer;
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

            if (!NavMesh.SamplePosition(target, out NavMeshHit ground, SampleReach, NavMesh.AllAreas))
            {
                Log.Info($"Entity {name} found no navmesh near {target}");
                Status = AgentMovementStatus.Unreachable;
                Finish(Snapshot(AgentMovementStatus.Unreachable, target));
                return;
            }

            Destination = ground.position;
            Status = AgentMovementStatus.Walking;
            agent.SetDestination(Destination);
            Log.Info($"Entity {name} going to {Destination}");
        }

        public void Interrupt(string interrupterName)
        {
            if (Status != AgentMovementStatus.Walking) return;

            Log.Info($"Entity {name} interrupted task {TaskName} by {interrupterName}");
            Status = AgentMovementStatus.Interrupted;
            agent.ResetPath();
            Finish(Snapshot(AgentMovementStatus.Interrupted, Destination, interrupterName));
        }

        public bool CanReach(Vector3 target)
        {
            return NavMesh.SamplePosition(target, out NavMeshHit ground, SampleReach, NavMesh.AllAreas)
                   && agent.CalculatePath(ground.position, scratch)
                   && scratch.status == NavMeshPathStatus.PathComplete;
        }

        protected override float Tick(float dt)
        {
            Vector3 position = transform.position;
            if (position != written) Place(position);

            if (Status == AgentMovementStatus.Walking && !agent.pathPending) Judge();

            bool walking = Status == AgentMovementStatus.Walking && !agent.pathPending;
            agent.speed = AffordSpeed(walking, Sprinting, dt);

            Vector3 velocity = agent.velocity;
            if (velocity.x != 0f || velocity.z != 0f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(velocity.x, 0f, velocity.z));
            }

            Vector3 next = agent.nextPosition;
            transform.position = next;
            written = next;
            return Vector2.Distance(new Vector2(position.x, position.z), new Vector2(next.x, next.z));
        }

        protected override void TeleportRaw(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        private void Place(Vector3 position)
        {
            if (!agent.Warp(position))
            {
                throw new InvalidOperationException($"Entity {name} stands off the navmesh at {position}");
            }

            if (Status == AgentMovementStatus.Walking) agent.SetDestination(Destination);
        }

        private void Judge()
        {
            if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Log.Info($"Entity {name} found no path to {Destination}");
                Status = AgentMovementStatus.Unreachable;
                agent.ResetPath();
                Finish(Snapshot(AgentMovementStatus.Unreachable, Destination));
                return;
            }

            if (agent.remainingDistance > agent.stoppingDistance) return;

            Log.Info($"Entity {name} arrived at {Destination}");
            Status = AgentMovementStatus.Arrived;
            agent.ResetPath();
            Finish(Snapshot(AgentMovementStatus.Arrived, Destination));
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

        private CallbackData Snapshot(AgentMovementStatus status, Vector3 destination, string interrupterName = null)
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
    }
}
