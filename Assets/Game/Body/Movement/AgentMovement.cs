using System;
using Shooter.Logging;
using Unity.AI.Navigation.LowLevel;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentMovement : Movement
    {
        private const float SampleReach = 2f;
        private const float LevelSlack = 1.5f;
        private const float BelowReach = 30f;
        private const int AskingLag = 1;

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float shortfallLimit = 50f;

        private NavMeshAgent agent;
        private NavMeshPath scratch;
        private Vector3 written;
        private int askedFrame = int.MinValue;

        public float ShortfallLimit => shortfallLimit;

        public AgentMovementStatus Status { get; private set; } = AgentMovementStatus.Idle;
        public bool Sprinting { get; private set; }
        public Vector3 Target { get; private set; }
        public Vector3 Destination { get; private set; }

        public Vector3 Feet => agent.nextPosition - Vector3.up * agent.baseOffset;

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

        public void Walk(Vector3 target, bool sprint)
        {
            askedFrame = Time.frameCount;
            Sprinting = sprint;

            if (target == Target && Status != AgentMovementStatus.Idle)
            {
                if (Status == AgentMovementStatus.Walking && !agent.hasPath && !agent.pathPending)
                {
                    Log.Info($"Entity {name} asks again for the path to {Destination}");
                    agent.SetDestination(Destination);
                }
                return;
            }

            Target = target;

            if (!NearestGround(target, SampleReach, out Vector3 ground))
            {
                Log.Info($"Entity {name} found no navmesh near {target}");
                Status = AgentMovementStatus.Unreachable;
                agent.ResetPath();
                return;
            }

            Destination = ground;
            Status = AgentMovementStatus.Walking;
            agent.SetDestination(Destination);
            Log.Info($"Entity {name} starts a walk to {Destination}");
        }

        public bool TryPlan(Vector3 target, out Vector3 end)
        {
            end = target;

            if (!NearestGround(target, SampleReach, out Vector3 ground)) return false;
            if (!agent.CalculatePath(ground, scratch) || scratch.status == NavMeshPathStatus.PathInvalid) return false;

            Vector3[] corners = scratch.corners;
            if (corners.Length == 0) return false;

            end = corners[corners.Length - 1];
            return true;
        }

        public static bool NearestGround(Vector3 position, float reach, out Vector3 ground)
        {
            NavWorld world = NavWorld.GetDefaultWorld();
            NavLocation found = world.MapLocation(position, new Vector3(reach, LevelSlack, reach), 0, NavMesh.AllAreas);
            if (!world.IsValid(found))
            {
                found = world.MapLocation(position + Vector3.down * (BelowReach / 2f), new Vector3(reach, BelowReach / 2f, reach), 0, NavMesh.AllAreas);
            }

            bool valid = world.IsValid(found);
            ground = valid ? found.position : position;
            return valid;
        }

        protected override float Tick(float dt)
        {
            Vector3 position = transform.position;
            if (position != written) Place(position);

            if (Status != AgentMovementStatus.Idle && Time.frameCount - askedFrame > AskingLag) Stand();
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
            Place(position);

            if (Status != AgentMovementStatus.Walking) return;

            Log.Info($"Entity {name} displaced, the walk to {Destination} is over");
            Status = AgentMovementStatus.Displaced;
            agent.ResetPath();
        }

        private void Place(Vector3 position)
        {
            if (!agent.Warp(position))
            {
                throw new InvalidOperationException($"Entity {name} stands off the navmesh at {position}");
            }

            written = position;
        }

        private void Stand()
        {
            if (Status == AgentMovementStatus.Walking) Log.Info($"Entity {name} stops the walk to {Destination}: nobody asks for it");

            Status = AgentMovementStatus.Idle;
            agent.ResetPath();
        }

        private void Judge()
        {
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Log.Info($"Entity {name} found no path to {Destination}");
                Status = AgentMovementStatus.Unreachable;
                agent.ResetPath();
                return;
            }

            if (!agent.hasPath) return;

            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                float shortfall = Vector3.Distance(agent.pathEndPosition, Destination);
                if (shortfall > shortfallLimit)
                {
                    Log.Info($"Entity {name} found no way to {Destination}: the nearest walkable ground is {shortfall:F0} m short of it");
                    Status = AgentMovementStatus.Unreachable;
                    agent.ResetPath();
                    return;
                }
            }

            if (agent.remainingDistance > agent.stoppingDistance) return;

            Log.Info($"Entity {name} finished the walk {Vector3.Distance(Feet, Destination):F1} m from {Destination}");
            Status = AgentMovementStatus.Arrived;
            agent.ResetPath();
        }
    }
}
