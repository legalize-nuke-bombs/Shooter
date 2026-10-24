using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class BulletHoles : ShooterNetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Material material;
        [SerializeField] private int capacity = 1024;
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
            RedrawAllHoles();
        }

        private readonly List<DecalProjector> projectors = new();

        private int nextHoleToOverwrite;

        public void AddHole(Vector3 position, Vector3 normal)
        {
            if (!IsServer) return;

            BulletHole hole = new() { Position = position, Normal = normal };

            if (holes.Count < capacity)
            {
                holes.Add(hole);
                PlaceHole(holes.Count - 1);
                return;
            }

            holes[nextHoleToOverwrite] = hole;
            PlaceHole(nextHoleToOverwrite);
            nextHoleToOverwrite = (nextHoleToOverwrite + 1) % capacity;
        }

        public override void OnNetworkSpawn()
        {
            if (material == null) Log.Error("Bullet holes have no material, nothing will be drawn");

            if (!IsServer) holes.OnListChanged += HandleHolesReplicated;
            RedrawAllHoles();

            Log.Info($"Bullet holes are up: {holes.Count} in the world, capacity {capacity}");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) holes.OnListChanged -= HandleHolesReplicated;

            foreach (DecalProjector projector in projectors)
            {
                projector.gameObject.SetActive(false);
            }
        }

        private void HandleHolesReplicated(NetworkListEvent<BulletHole> change)
        {
            switch (change.Type)
            {
                case NetworkListEvent<BulletHole>.EventType.Add:
                case NetworkListEvent<BulletHole>.EventType.Insert:
                case NetworkListEvent<BulletHole>.EventType.Value:
                    PlaceHole(change.Index);
                    break;
                default:
                    RedrawAllHoles();
                    break;
            }
        }

        private void RedrawAllHoles()
        {
            for (int i = 0; i < holes.Count; i++)
            {
                PlaceHole(i);
            }

            for (int i = holes.Count; i < projectors.Count; i++)
            {
                projectors[i].gameObject.SetActive(false);
            }
        }

        private void PlaceHole(int index)
        {
            if (material == null) return;

            BulletHole hole = holes[index];
            DecalProjector projector = GetOrCreateProjector(index);
            int seed = HashPosition(hole.Position);

            Vector3 upAxis = Mathf.Abs(hole.Normal.y) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(-hole.Normal, upAxis) * Quaternion.Euler(0f, 0f, seed % 360);
            projector.transform.SetPositionAndRotation(hole.Position, rotation);

            float scale = 0.8f + ((seed >> 2) & 15) / 30f;
            projector.size = new Vector3(diameter * scale, diameter * scale, depth);
            projector.uvScale = new Vector2(0.5f, 0.5f);
            projector.uvBias = new Vector2((seed & 1) * 0.5f, ((seed >> 1) & 1) * 0.5f);

            projector.gameObject.SetActive(true);
        }

        private DecalProjector GetOrCreateProjector(int index)
        {
            while (projectors.Count <= index)
            {
                GameObject projectorObject = new($"BulletHole{projectors.Count}");
                projectorObject.transform.SetParent(transform, false);
                projectorObject.SetActive(false);

                DecalProjector projector = projectorObject.AddComponent<DecalProjector>();
                projector.material = material;
                projector.pivot = Vector3.zero;

                projectors.Add(projector);
            }

            return projectors[index];
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
