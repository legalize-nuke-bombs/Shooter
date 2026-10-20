using Shooter.Infrastructure;
using Unity.AI.Navigation.LowLevel;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentMovement : Movement
    {
        private const float SampleReach = 2f;
        private const float LevelSlack = 1.5f;
        private const float BelowReach = 30f;
        private const int WalkRequestLagFrames = 1;

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float shortfallLimit = 50f;

        private NavMeshAgent navMeshAgent;
        private NavMeshPath plannedPath;
        private Vector3 lastWrittenPosition;
        private bool stranded;
        private int lastWalkRequestFrame = int.MinValue;

        public float ShortfallLimit => shortfallLimit;

        public AgentMovementStatus Status { get; private set; } = AgentMovementStatus.Idle;
        public bool IsSprinting { get; private set; }
        public Vector3 Target { get; private set; }
        public Vector3 Destination { get; private set; }

        public Vector3 FeetPosition => navMeshAgent.nextPosition - Vector3.up * navMeshAgent.baseOffset;

        protected override void ShooterAwake()
        {
            base.ShooterAwake();

            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            plannedPath = new NavMeshPath();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            navMeshAgent.enabled = IsServer;
        }

        public void WalkTo(Vector3 target, bool sprint)
        {
            lastWalkRequestFrame = Time.frameCount;
            IsSprinting = sprint;

            if (target == Target && Status != AgentMovementStatus.Idle)
            {
                if (Status == AgentMovementStatus.Walking && !navMeshAgent.hasPath && !navMeshAgent.pathPending)
                {
                    Log.Info($"Entity {name} asks again for the path to {Destination}");
                    navMeshAgent.SetDestination(Destination);
                }
                return;
            }

            Target = target;

            if (!TryFindNearestGround(target, SampleReach, out Vector3 ground))
            {
                Log.Info($"Entity {name} found no navmesh near {target}");
                Status = AgentMovementStatus.Unreachable;
                navMeshAgent.ResetPath();
                return;
            }

            Destination = ground;
            Status = AgentMovementStatus.Walking;
            navMeshAgent.SetDestination(Destination);
            Log.Info($"Entity {name} starts a walk to {Destination}");
        }

        public bool TryPlanPathEnd(Vector3 target, out Vector3 pathEnd)
        {
            pathEnd = target;

            if (!TryFindNearestGround(target, SampleReach, out Vector3 ground)) return false;
            if (!navMeshAgent.CalculatePath(ground, plannedPath) || plannedPath.status == NavMeshPathStatus.PathInvalid) return false;

            Vector3[] corners = plannedPath.corners;
            if (corners.Length == 0) return false;

            pathEnd = corners[corners.Length - 1];
            return true;
        }

        public static bool TryFindNearestGround(Vector3 position, float reach, out Vector3 ground)
        {
            NavWorld navWorld = NavWorld.GetDefaultWorld();
            NavLocation location = navWorld.MapLocation(position, new Vector3(reach, LevelSlack, reach), 0, NavMesh.AllAreas);
            if (!navWorld.IsValid(location))
            {
                location = navWorld.MapLocation(position + Vector3.down * (BelowReach / 2f), new Vector3(reach, BelowReach / 2f, reach), 0, NavMesh.AllAreas);
            }

            bool found = navWorld.IsValid(location);
            ground = found ? location.position : position;
            return found;
        }

        protected override float MoveForTick(float dt)
        {
            Vector3 position = transform.position;
            if ((position != lastWrittenPosition || stranded) && !TryWarpAgent(position)) return 0f;

            if (Status != AgentMovementStatus.Idle && Time.frameCount - lastWalkRequestFrame > WalkRequestLagFrames) StopWalking();
            if (Status == AgentMovementStatus.Walking && !navMeshAgent.pathPending) UpdateWalkStatusFromPath();

            bool walking = Status == AgentMovementStatus.Walking && !navMeshAgent.pathPending;
            navMeshAgent.speed = AffordSpeed(walking, IsSprinting, dt);

            Vector3 velocity = navMeshAgent.velocity;
            if (velocity.x != 0f || velocity.z != 0f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(velocity.x, 0f, velocity.z));
            }

            Vector3 nextPosition = navMeshAgent.nextPosition;
            transform.position = nextPosition;
            lastWrittenPosition = nextPosition;
            return Vector2.Distance(new Vector2(position.x, position.z), new Vector2(nextPosition.x, nextPosition.z));
        }

        protected override void TeleportLocally(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            TryWarpAgent(position);

            if (Status != AgentMovementStatus.Walking) return;

            Log.Info($"Entity {name} displaced, the walk to {Destination} is over");
            Status = AgentMovementStatus.Displaced;
            navMeshAgent.ResetPath();
        }

        private bool TryWarpAgent(Vector3 position)
        {
            lastWrittenPosition = position;
            if (!navMeshAgent.Warp(position))
            {
                if (!stranded) Log.Error($"Entity {name} stands off the navmesh at {position}, it stays put until it is back on it");
                stranded = true;
                return false;
            }

            if (stranded) Log.Info($"Entity {name} is back on the navmesh at {position}");
            stranded = false;
            return true;
        }

        private void StopWalking()
        {
            if (Status == AgentMovementStatus.Walking) Log.Info($"Entity {name} stops the walk to {Destination}: nobody asks for it");

            Status = AgentMovementStatus.Idle;
            navMeshAgent.ResetPath();
        }

        private void UpdateWalkStatusFromPath()
        {
            if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Log.Info($"Entity {name} found no path to {Destination}");
                Status = AgentMovementStatus.Unreachable;
                navMeshAgent.ResetPath();
                return;
            }

            if (!navMeshAgent.hasPath) return;

            if (navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                float shortfall = Vector3.Distance(navMeshAgent.pathEndPosition, Destination);
                if (shortfall > shortfallLimit)
                {
                    Log.Info($"Entity {name} found no way to {Destination}: the nearest walkable ground is {shortfall:F0} m short of it");
                    Status = AgentMovementStatus.Unreachable;
                    navMeshAgent.ResetPath();
                    return;
                }
            }

            if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance) return;

            Log.Info($"Entity {name} finished the walk {Vector3.Distance(FeetPosition, Destination):F1} m from {Destination}");
            Status = AgentMovementStatus.Arrived;
            navMeshAgent.ResetPath();
        }
    }
}
