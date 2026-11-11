using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.AI.Navigation.LowLevel;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentMovement : Movement
    {
        private const float SampleReach = 2f;
        private const float LevelSlack = 1.5f;
        private const float BelowReach = 30f;
        private const int WalkRequestLagFrames = 1;
        private const float CornerReach = 0.1f;

        private static readonly Journal Log = Logs.Here();
        private static readonly Vector3 LocationExtents = new(SampleReach, LevelSlack, SampleReach);

        [SerializeField] private float shortfallLimit = 50f;
        [SerializeField] private int maxPathSearchNodes = 4096;

        private readonly List<Vector3> corners = new();
        private readonly List<Vector3> plannedCorners = new();
        private NavMeshAgent navMeshAgent;
        private int nextCorner;
        private Vector3 lastWrittenPosition;
        private bool stranded;
        private int lastWalkRequestFrame = int.MinValue;

        public float ShortfallLimit => shortfallLimit;

        public AgentMovementStatus Status { get; private set; } = AgentMovementStatus.Idle;
        public bool IsSprinting { get; private set; }
        public Vector3 Target { get; private set; }
        public Vector3 Destination { get; private set; }

        public Vector3 FeetPosition => navMeshAgent.nextPosition - Vector3.up * navMeshAgent.baseOffset;

        protected override void Awake()
        {
            base.Awake();

            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            ShooterEntity.SetEnabled(navMeshAgent, IsServer);
        }

        public void WalkTo(Vector3 target, bool sprint)
        {
            lastWalkRequestFrame = Time.frameCount;
            IsSprinting = sprint;

            if (target == Target && Status != AgentMovementStatus.Idle) return;

            Target = target;

            if (!TryFindNearestGround(target, SampleReach, out Vector3 ground))
            {
                Log.Info($"Entity {name} found no navmesh near {target}");
                Status = AgentMovementStatus.Unreachable;
                corners.Clear();
                return;
            }

            Destination = ground;
            PlanWalk();
        }

        public bool TryPlanPathEnd(Vector3 target, out Vector3 pathEnd)
        {
            pathEnd = target;

            if (!TryFindNearestGround(target, SampleReach, out Vector3 ground)) return false;
            if (FindPath(ground, plannedCorners) == NavMeshPathStatus.PathInvalid) return false;

            pathEnd = plannedCorners[^1];
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
            if (position != lastWrittenPosition || stranded)
            {
                if (!TryWarpAgent(position))
                {
                    if (Status == AgentMovementStatus.Walking)
                    {
                        Log.Info($"Entity {name} can not walk to {Destination}: it stands off the navmesh");
                        Status = AgentMovementStatus.Unreachable;
                        corners.Clear();
                    }
                    return 0f;
                }

                if (Status == AgentMovementStatus.Walking)
                {
                    Log.Info($"Entity {name} was moved during the walk, plans the path to {Destination} from {position}");
                    PlanWalk();
                }
            }

            if (Status != AgentMovementStatus.Idle && Time.frameCount - lastWalkRequestFrame > WalkRequestLagFrames) StopWalking();

            float speed = AffordSpeed(Status == AgentMovementStatus.Walking, IsSprinting, dt);
            if (Status == AgentMovementStatus.Walking) WalkAlongPath(speed * dt);

            Vector3 nextPosition = navMeshAgent.nextPosition;
            Vector3 step = new(nextPosition.x - position.x, 0f, nextPosition.z - position.z);
            if (step != Vector3.zero) transform.rotation = Quaternion.LookRotation(step);

            transform.position = nextPosition;
            lastWrittenPosition = nextPosition;
            return step.magnitude;
        }

        protected override void TeleportLocally(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            TryWarpAgent(position);

            if (Status != AgentMovementStatus.Walking) return;

            Log.Info($"Entity {name} displaced, the walk to {Destination} is over");
            Status = AgentMovementStatus.Displaced;
            corners.Clear();
        }

        private NavMeshPathStatus FindPath(Vector3 to, List<Vector3> path)
        {
            return NavMeshPathfinder.FindPath(FeetPosition, to, navMeshAgent.agentTypeID, maxPathSearchNodes, LocationExtents, path);
        }

        private void PlanWalk()
        {
            nextCorner = 0;

            NavMeshPathStatus pathStatus = FindPath(Destination, corners);
            if (pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Log.Info($"Entity {name} found no path to {Destination}");
                Status = AgentMovementStatus.Unreachable;
                corners.Clear();
                return;
            }

            float shortfall = Vector3.Distance(corners[^1], Destination);
            if (pathStatus == NavMeshPathStatus.PathPartial && shortfall > shortfallLimit)
            {
                Log.Info($"Entity {name} found no way to {Destination}: the nearest walkable ground is {shortfall:F0} m short of it");
                Status = AgentMovementStatus.Unreachable;
                corners.Clear();
                return;
            }

            Status = AgentMovementStatus.Walking;
            Log.Info($"Entity {name} starts a walk to {Destination}: {corners.Count} corners, {PathLength():F0} m");
        }

        private float PathLength()
        {
            float length = 0f;
            Vector3 from = FeetPosition;
            foreach (Vector3 corner in corners)
            {
                length += Vector3.Distance(from, corner);
                from = corner;
            }
            return length;
        }

        private void WalkAlongPath(float distance)
        {
            while (nextCorner < corners.Count)
            {
                Vector3 toCorner = corners[nextCorner] - FeetPosition;
                toCorner.y = 0f;
                float cornerDistance = toCorner.magnitude;
                if (cornerDistance > distance)
                {
                    navMeshAgent.Move(toCorner * (distance / cornerDistance));
                    return;
                }

                navMeshAgent.Move(toCorner);
                if (FlatDistance(FeetPosition, corners[nextCorner]) > CornerReach) return;

                distance -= cornerDistance;
                nextCorner++;
            }

            Log.Info($"Entity {name} finished the walk {Vector3.Distance(FeetPosition, Destination):F1} m from {Destination}");
            Status = AgentMovementStatus.Arrived;
            corners.Clear();
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
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
            corners.Clear();
        }
    }
}
