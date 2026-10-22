// Grown from #NVJOB Simple Boids by Nicholas Veselov and Unity Technologies, MIT, licenses in Data/Environment/Flocks.

using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Flocks : ShooterNetworkBehaviour
    {
        private const float FullEscapeFear = 0.5f;
        private const float NewEscapeFearMargin = 0.25f;
        private const float ScaredFlappingPanic = 0.2f;
        private static readonly int FlapSpeedId = Shader.PropertyToID("_Flap_Speed");

        [SerializeField] private GameObject boidPrefab;

        [SerializeField] [Min(1)] private int flockCount = 2;

        [SerializeField] [Min(1)] private int boidCount = 10;

        [SerializeField] private float flockSpread = 30f;

        [SerializeField] [Range(0f, 1f)] private float flockSpreadVertical = 0.5f;

        [SerializeField] private float flockSmoothTime = 0.5f;

        [SerializeField] private float flockMaxSpeed = 4f;

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

        [SerializeField] private float scaredSpeedMultiplier = 1.7f;

        [SerializeField] private float speedRampTime = 0.5f;

        [SerializeField] private float scaredTurnMultiplier = 16f;

        [SerializeField] [Range(0f, 90f)] private float scaredMaxPitch = 40f;

        [SerializeField] [Range(0f, 180f)] private float escapeSpread = 15f;

        [SerializeField] private Vector2 escapeClimbRange = new(15f, 35f);

        [SerializeField] private float scaredFlapMultiplier = 2.5f;

        [SerializeField] private float scaredClimb = 30f;

        [SerializeField] private float calmDownTime = 10f;

        private Vector3[] flockPoints;
        private Vector3[] flockTargets;
        private Vector3[] flockVelocities;
        private Vector3[] flockWorldPoints;
        private float[] flockFleeingUntil;

        private Transform[] boids;
        private Renderer[] boidRenderers;
        private int[] boidFlocks;
        private Vector3[] boidOffsets;
        private float[] boidSpeeds;
        private float[] boidFears;
        private float[] boidPanics;
        private Vector3[] boidEscapes;
        private bool[] boidFlappingScared;

        private Material calmMaterial;
        private Material scaredMaterial;
        private float waveTime;
        private float nextRetargetTime;

        protected override void ShooterAwake()
        {
            CreateFlocks();
            CreateBoids();

            calmMaterial = boidRenderers[0].sharedMaterial;
            scaredMaterial = new Material(calmMaterial);
            scaredMaterial.SetFloat(FlapSpeedId, calmMaterial.GetFloat(FlapSpeedId) * scaredFlapMultiplier);

            nextRetargetTime = Time.time + Random.Range(retargetInterval.x, retargetInterval.y);
        }

        protected override void ShooterOnDestroy()
        {
            Destroy(scaredMaterial);
        }

        private void LateUpdate()
        {
            if (Time.time >= nextRetargetTime) Retarget();

            MoveFlocks();
            MoveBoids(Time.deltaTime);
        }

        [Rpc(SendTo.Everyone)]
        public void ScareRpc(Vector3 origin, float radius)
        {
            ScareBoids(origin, radius);
            ScareFlocks(origin, radius);
        }

        private void ScareBoids(Vector3 origin, float radius)
        {
            for (int boid = 0; boid < boidCount; boid++)
            {
                Vector3 away = boids[boid].position - origin;
                float fear = 1f - away.magnitude / radius;
                if (fear <= 0f) continue;

                if (boidFears[boid] <= 0f || fear > boidFears[boid] + NewEscapeFearMargin) boidEscapes[boid] = PickEscape(away);
                boidFears[boid] = Mathf.Max(boidFears[boid], fear);
            }
        }

        private void ScareFlocks(Vector3 origin, float radius)
        {
            Vector3 localOrigin = transform.InverseTransformPoint(origin);

            for (int flock = 0; flock < flockCount; flock++)
            {
                Vector3 away = flockPoints[flock] - localOrigin;
                float fear = 1f - away.magnitude / radius;
                if (fear <= 0f) continue;

                Vector3 refuge = PickFlatDirection(away) * flockSpread;
                flockTargets[flock] = new Vector3(refuge.x, Mathf.Max(flockTargets[flock].y, scaredClimb * fear), refuge.z);

                float fleeTime = Vector3.Distance(flockPoints[flock], flockTargets[flock]) / (flockMaxSpeed * scaredSpeedMultiplier);
                flockFleeingUntil[flock] = Time.time + Mathf.Max(calmDownTime, fleeTime);
            }
        }

        private Vector3 PickEscape(Vector3 away)
        {
            Vector3 heading = Quaternion.Euler(0f, Random.Range(-escapeSpread, escapeSpread), 0f) * PickFlatDirection(away);
            float climb = Random.Range(escapeClimbRange.x, escapeClimbRange.y) * Mathf.Deg2Rad;
            return heading * Mathf.Cos(climb) + Vector3.up * Mathf.Sin(climb);
        }

        private static Vector3 PickFlatDirection(Vector3 away)
        {
            Vector3 flat = new(away.x, 0f, away.z);
            return flat == Vector3.zero ? Quaternion.Euler(0f, Random.value * 360f, 0f) * Vector3.forward : flat.normalized;
        }

        private void CreateFlocks()
        {
            flockPoints = new Vector3[flockCount];
            flockTargets = new Vector3[flockCount];
            flockVelocities = new Vector3[flockCount];
            flockWorldPoints = new Vector3[flockCount];
            flockFleeingUntil = new float[flockCount];

            for (int flock = 0; flock < flockCount; flock++)
            {
                flockPoints[flock] = PickFlockPoint(Random.onUnitSphere);
                flockTargets[flock] = flockPoints[flock];
            }
        }

        private void CreateBoids()
        {
            boids = new Transform[boidCount];
            boidRenderers = new Renderer[boidCount];
            boidFlocks = new int[boidCount];
            boidOffsets = new Vector3[boidCount];
            boidSpeeds = new float[boidCount];
            boidFears = new float[boidCount];
            boidPanics = new float[boidCount];
            boidEscapes = new Vector3[boidCount];
            boidFlappingScared = new bool[boidCount];

            for (int boid = 0; boid < boidCount; boid++)
            {
                boidFlocks[boid] = Random.Range(0, flockCount);
                boidOffsets[boid] = PickBoidOffset();
                boidSpeeds[boid] = Random.Range(speedRange.x, speedRange.y);

                boids[boid] = Instantiate(boidPrefab, transform).transform;
                boids[boid].localPosition = flockPoints[boidFlocks[boid]] + boidOffsets[boid];
                boids[boid].localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
                boids[boid].localRotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
                boidRenderers[boid] = boids[boid].GetComponentInChildren<Renderer>();
            }
        }

        private void Retarget()
        {
            nextRetargetTime = Time.time + Random.Range(retargetInterval.x, retargetInterval.y);

            for (int flock = 0; flock < flockCount; flock++)
            {
                if (Time.time < flockFleeingUntil[flock] || Random.value >= flockRetargetChance) continue;

                flockTargets[flock] = PickFlockPoint(Random.insideUnitSphere);
            }

            for (int boid = 0; boid < boidCount; boid++)
            {
                boidSpeeds[boid] = Random.Range(speedRange.x, speedRange.y);
                boidOffsets[boid] = PickBoidOffset();
                if (Random.value < migrationChance) boidFlocks[boid] = Random.Range(0, flockCount);
            }
        }

        private void MoveFlocks()
        {
            for (int flock = 0; flock < flockCount; flock++)
            {
                float maxSpeed = Time.time < flockFleeingUntil[flock] ? flockMaxSpeed * scaredSpeedMultiplier : flockMaxSpeed;
                flockPoints[flock] = Vector3.SmoothDamp(flockPoints[flock], flockTargets[flock], ref flockVelocities[flock], flockSmoothTime, maxSpeed);
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
                float fear = boidFears[boid] = Mathf.Max(0f, boidFears[boid] - deltaTime / calmDownTime);
                float panic = boidPanics[boid] = Mathf.MoveTowards(boidPanics[boid], fear, deltaTime / speedRampTime);
                SetFlappingScared(boid, panic > ScaredFlappingPanic);

                boidTransform.position += boidTransform.forward * (boidSpeeds[boid] * Mathf.Lerp(1f, scaredSpeedMultiplier, panic) * deltaTime);

                Vector3 toFlock = (flockWorldPoints[boidFlocks[boid]] + boidOffsets[boid] + wave - boidTransform.position).normalized;
                Vector3 heading = fear > 0f ? Vector3.Slerp(toFlock, boidEscapes[boid], Mathf.Clamp01(fear / FullEscapeFear)) : toFlock;
                float turn = turnSpeed * Mathf.Lerp(1f, scaredTurnMultiplier, fear) * deltaTime;
                Vector3 euler = Quaternion.LookRotation(Vector3.RotateTowards(boidTransform.forward, heading, turn, 0f)).eulerAngles;
                float pitchLimit = Mathf.Lerp(maxPitch, scaredMaxPitch, fear);

                boidTransform.rotation = Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -pitchLimit, pitchLimit), euler.y, 0f);
            }
        }

        private void SetFlappingScared(int boid, bool scared)
        {
            if (boidFlappingScared[boid] == scared) return;

            boidFlappingScared[boid] = scared;
            boidRenderers[boid].sharedMaterial = scared ? scaredMaterial : calmMaterial;
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
