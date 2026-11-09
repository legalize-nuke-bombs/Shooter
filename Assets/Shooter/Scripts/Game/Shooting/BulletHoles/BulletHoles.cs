using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class BulletHoles : NetworkBehaviour, ISaveableComponent, IShooterLateUpdateable
    {
        private const int NoProjector = -1;
        private const int NoHole = -1;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Material material;
        [SerializeField] private int capacity = 25000;
        [SerializeField] private int projectorCount = 512;
        [SerializeField] private float diameter = 0.08f;
        [SerializeField] private float depth = 0.2f;

        private readonly NetworkList<BulletHole> holes = new();

        private struct SaveData
        {
            public List<BulletHole> Holes { get; set; }
        }
        public object SaveObject()
        {
            SaveData saveData = new()
            {
                Holes = new List<BulletHole>(holes.Count)
            };
            // Oldest first, so that after loading the next hole to overwrite is the first one
            for (int age = 0; age < holes.Count; age++)
            {
                saveData.Holes.Add(holes[(nextHoleToOverwrite + age) % holes.Count]);
            }
            return saveData;
        }
        public void LoadObject(SaveToken token)
        {
            SaveData saveData = token.ToObject<SaveData>();
            holes.Clear();
            nextHoleToOverwrite = 0;
            foreach (BulletHole hole in saveData.Holes)
            {
                holes.Add(hole);
            }
            FreeAllProjectors();
        }

        private readonly List<DecalProjector> projectors = new();

        private int[] holeOfProjector;
        private int[] projectorOfHole;
        private int[] projectorHeap;
        private float[] projectorDistance;
        private int nextHoleToOverwrite;

        private void Awake()
        {
            projectorOfHole = new int[capacity];
            holeOfProjector = new int[projectorCount];
            projectorHeap = new int[projectorCount];
            projectorDistance = new float[projectorCount];
            for (int projector = 0; projector < projectorCount; projector++)
            {
                projectors.Add(CreateProjector(projector));
            }

            FreeAllProjectors();
        }

        public void AddHole(Vector3 position, Vector3 normal)
        {
            if (!IsServer) return;

            BulletHole hole = new() { Position = position, Normal = normal };

            if (holes.Count < capacity)
            {
                holes.Add(hole);
                return;
            }

            holes[nextHoleToOverwrite] = hole;
            RedrawIfShown(nextHoleToOverwrite);
            nextHoleToOverwrite = (nextHoleToOverwrite + 1) % capacity;
        }

        public override void OnNetworkSpawn()
        {
            if (material == null) Log.Error("Bullet holes have no material, nothing will be drawn");

            if (!IsServer) holes.OnListChanged += HandleHolesReplicated;

            Log.Info($"Bullet holes are up: {holes.Count} in the world, capacity {capacity}, {projectorCount} drawn around the camera");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) holes.OnListChanged -= HandleHolesReplicated;

            FreeAllProjectors();
        }

        public void ShooterLateUpdate()
        {
            Camera eye = Camera.main;
            if (eye == null || material == null) return;

            Vector3 eyePosition = eye.transform.position;
            BuildProjectorHeap(eyePosition);
            for (int hole = 0; hole < holes.Count; hole++)
            {
                if (projectorOfHole[hole] != NoProjector) continue;

                float distance = (holes[hole].Position - eyePosition).sqrMagnitude;
                int farthestProjector = projectorHeap[0];
                if (distance >= projectorDistance[farthestProjector]) continue;

                ShowHole(farthestProjector, hole);
                projectorDistance[farthestProjector] = distance;
                SiftDown(0);
            }
        }

        private void BuildProjectorHeap(Vector3 eyePosition)
        {
            for (int projector = 0; projector < projectorHeap.Length; projector++)
            {
                int hole = holeOfProjector[projector];
                projectorDistance[projector] = hole == NoHole ? float.PositiveInfinity : (holes[hole].Position - eyePosition).sqrMagnitude;
                projectorHeap[projector] = projector;
            }

            for (int node = projectorHeap.Length / 2 - 1; node >= 0; node--)
            {
                SiftDown(node);
            }
        }

        private void SiftDown(int node)
        {
            while (true)
            {
                int farthest = node;
                int left = 2 * node + 1;
                int right = left + 1;
                if (left < projectorHeap.Length && projectorDistance[projectorHeap[left]] > projectorDistance[projectorHeap[farthest]]) farthest = left;
                if (right < projectorHeap.Length && projectorDistance[projectorHeap[right]] > projectorDistance[projectorHeap[farthest]]) farthest = right;
                if (farthest == node) return;

                (projectorHeap[node], projectorHeap[farthest]) = (projectorHeap[farthest], projectorHeap[node]);
                node = farthest;
            }
        }

        private void HandleHolesReplicated(NetworkListEvent<BulletHole> change)
        {
            switch (change.Type)
            {
                case NetworkListEvent<BulletHole>.EventType.Add:
                    break;
                case NetworkListEvent<BulletHole>.EventType.Value:
                    RedrawIfShown(change.Index);
                    break;
                default:
                    FreeAllProjectors();
                    break;
            }
        }

        private void ShowHole(int projector, int hole)
        {
            int shownHole = holeOfProjector[projector];
            if (shownHole != NoHole) projectorOfHole[shownHole] = NoProjector;

            holeOfProjector[projector] = hole;
            projectorOfHole[hole] = projector;
            PlaceProjector(projectors[projector], holes[hole]);
        }

        private void RedrawIfShown(int hole)
        {
            int projector = projectorOfHole[hole];
            if (projector != NoProjector) PlaceProjector(projectors[projector], holes[hole]);
        }

        private void FreeAllProjectors()
        {
            Array.Fill(projectorOfHole, NoProjector);
            for (int projector = 0; projector < projectors.Count; projector++)
            {
                holeOfProjector[projector] = NoHole;
                projectors[projector].gameObject.SetActive(false);
            }
        }

        private DecalProjector CreateProjector(int index)
        {
            GameObject projectorObject = new($"BulletHole{index}");
            projectorObject.transform.SetParent(transform, false);
            projectorObject.SetActive(false);

            DecalProjector projector = projectorObject.AddComponent<DecalProjector>();
            projector.material = material;
            projector.pivot = Vector3.zero;
            projector.uvScale = new Vector2(0.5f, 0.5f);
            return projector;
        }

        private void PlaceProjector(DecalProjector projector, BulletHole hole)
        {
            int seed = HashPosition(hole.Position);

            Vector3 upAxis = Mathf.Abs(hole.Normal.y) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(-hole.Normal, upAxis) * Quaternion.Euler(0f, 0f, seed % 360);
            projector.transform.SetPositionAndRotation(hole.Position, rotation);

            float scale = 0.8f + ((seed >> 2) & 15) / 30f;
            projector.size = new Vector3(diameter * scale, diameter * scale, depth);
            projector.uvBias = new Vector2((seed & 1) * 0.5f, ((seed >> 1) & 1) * 0.5f);

            projector.gameObject.SetActive(true);
        }

        private static int HashPosition(Vector3 position)
        {
            unchecked
            {
                int seed = position.x.GetHashCode();
                seed = seed * 31 + position.y.GetHashCode();
                seed = seed * 31 + position.z.GetHashCode();
                return seed;
            }
        }
    }
}
