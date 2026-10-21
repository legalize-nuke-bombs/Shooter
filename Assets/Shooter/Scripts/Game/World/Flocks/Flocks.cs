// Grown from #NVJOB Simple Boids by Nicholas Veselov and Unity Technologies, MIT, licenses in Data/Environment/Flocks.

using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Flocks : ShooterNetworkBehaviour
    {
        [SerializeField] private GameObject boidPrefab;

        [SerializeField] [Min(1)] private int flockCount = 2;

        [SerializeField] [Min(1)] private int boidCount = 10;

        [SerializeField] private float flockSpread = 30f;

        [SerializeField] [Range(0f, 1f)] private float flockSpreadVertical = 0.5f;

        [SerializeField] private float flockSmoothTime = 0.5f;

        [SerializeField] private float boidSpread = 10f;

        [SerializeField] [Range(0f, 1f)] private float boidSpreadVertical = 1f;

        [SerializeField] private Vector2 speedRange = new(3f, 7f);

        [SerializeField] private float turnSpeed = 0.5f;

        [SerializeField] [Range(0f, 90f)] private float maxPitch = 90f;

        [SerializeField] private float verticalWave = 20f;

        [SerializeField] private Vector2 scaleRange = new(1f, 1.5f);

        [SerializeField] private Vector2 retargetInterval = new(2f, 6f);

        [SerializeField] [Range(0f, 1f)] private float flockRetargetChance = 0.5f;

        [SerializeField] [Range(0f, 1f)] private float migrationChance = 0.1f;

        [SerializeField] private float scaredSpeedMultiplier = 2.5f;

        [SerializeField] private float scaredTurnMultiplier = 4f;

        [SerializeField] private float scaredSpreadMultiplier = 2f;

        [SerializeField] private float scaredClimb = 30f;

        [SerializeField] private float calmDownTime = 10f;

        private Vector3[] flockPoints;
        private Vector3[] flockTargets;
        private Vector3[] flockVelocities;
        private Vector3[] flockWorldPoints;
        private float[] flockFears;

        private Transform[] boids;
        private int[] boidFlocks;
        private Vector3[] boidOffsets;
        private float[] boidSpeeds;

        private float waveTime;
        private float nextRetargetTime;

        protected override void ShooterAwake()
        {
            CreateFlocks();
            CreateBoids();
            nextRetargetTime = Time.time + Random.Range(retargetInterval.x, retargetInterval.y);
        }

        private void LateUpdate()
        {
            if (Time.time >= nextRetargetTime) Retarget();

            MoveFlocks(Time.deltaTime);
            MoveBoids(Time.deltaTime);
        }

        [Rpc(SendTo.Everyone)]
        public void ScareRpc(Vector3 origin, float radius)
        {
            Vector3 localOrigin = transform.InverseTransformPoint(origin);

            for (int flock = 0; flock < flockCount; flock++)
            {
                Vector3 away = flockPoints[flock] - localOrigin;
                float fear = 1f - away.magnitude / radius;
                if (fear <= 0f) continue;

                flockFears[flock] = Mathf.Max(flockFears[flock], fear);

                Vector3 flight = new(away.x, 0f, away.z);
                if (flight == Vector3.zero) flight = Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.forward;

                Vector3 refuge = localOrigin + flight.normalized * radius;
                flockTargets[flock] = new Vector3(refuge.x, Mathf.Max(flockTargets[flock].y, scaredClimb * fear), refuge.z);
            }
        }

        private void CreateFlocks()
        {
            flockPoints = new Vector3[flockCount];
            flockTargets = new Vector3[flockCount];
            flockVelocities = new Vector3[flockCount];
            flockWorldPoints = new Vector3[flockCount];
            flockFears = new float[flockCount];

            for (int flock = 0; flock < flockCount; flock++)
            {
                flockPoints[flock] = PickFlockPoint(Random.onUnitSphere);
                flockTargets[flock] = flockPoints[flock];
            }
        }

        private void CreateBoids()
        {
            boids = new Transform[boidCount];
            boidFlocks = new int[boidCount];
            boidOffsets = new Vector3[boidCount];
            boidSpeeds = new float[boidCount];

            for (int boid = 0; boid < boidCount; boid++)
            {
                boidFlocks[boid] = Random.Range(0, flockCount);
                boidOffsets[boid] = PickBoidOffset();
                boidSpeeds[boid] = Random.Range(speedRange.x, speedRange.y);

                boids[boid] = Instantiate(boidPrefab, transform).transform;
                boids[boid].localPosition = flockPoints[boidFlocks[boid]] + boidOffsets[boid];
                boids[boid].localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
                boids[boid].localRotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
            }
        }

        private void Retarget()
        {
            nextRetargetTime = Time.time + Random.Range(retargetInterval.x, retargetInterval.y);

            for (int flock = 0; flock < flockCount; flock++)
            {
                if (flockFears[flock] > 0f || Random.value >= flockRetargetChance) continue;

                flockTargets[flock] = PickFlockPoint(Random.insideUnitSphere);
            }

            for (int boid = 0; boid < boidCount; boid++)
            {
                boidSpeeds[boid] = Random.Range(speedRange.x, speedRange.y);
                boidOffsets[boid] = PickBoidOffset();
                if (Random.value < migrationChance) boidFlocks[boid] = Random.Range(0, flockCount);
            }
        }

        private void MoveFlocks(float deltaTime)
        {
            for (int flock = 0; flock < flockCount; flock++)
            {
                flockFears[flock] = Mathf.Max(0f, flockFears[flock] - deltaTime / calmDownTime);
                flockPoints[flock] = Vector3.SmoothDamp(flockPoints[flock], flockTargets[flock], ref flockVelocities[flock], flockSmoothTime);
                flockWorldPoints[flock] = transform.TransformPoint(flockPoints[flock]);
            }
        }

        private void MoveBoids(float deltaTime)
        {
            waveTime += deltaTime;
            Vector3 wave = Vector3.up * (verticalWave * 0.5f - Mathf.PingPong(waveTime * 0.5f, verticalWave));

            for (int boid = 0; boid < boidCount; boid++)
            {
                Transform boidTransform = boids[boid];
                int flock = boidFlocks[boid];
                float fear = flockFears[flock];

                boidTransform.position += boidTransform.forward * (boidSpeeds[boid] * Mathf.Lerp(1f, scaredSpeedMultiplier, fear) * deltaTime);

                Vector3 target = flockWorldPoints[flock] + boidOffsets[boid] * Mathf.Lerp(1f, scaredSpreadMultiplier, fear) + wave;
                float turn = turnSpeed * Mathf.Lerp(1f, scaredTurnMultiplier, fear) * deltaTime;
                Vector3 heading = Quaternion.LookRotation(Vector3.RotateTowards(boidTransform.forward, target - boidTransform.position, turn, 0f)).eulerAngles;

                boidTransform.rotation = Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0f, heading.x), -maxPitch, maxPitch), heading.y, 0f);
            }
        }

        private Vector3 PickFlockPoint(Vector3 direction)
        {
            Vector3 point = direction * flockSpread;
            return new Vector3(point.x, Mathf.Abs(point.y * flockSpreadVertical), point.z);
        }

        private Vector3 PickBoidOffset()
        {
            Vector3 offset = Random.insideUnitSphere * boidSpread;
            return new Vector3(offset.x, offset.y * boidSpreadVertical, offset.z);
        }
    }
}
