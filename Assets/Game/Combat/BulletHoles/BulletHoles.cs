using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game.Combat
{
    public class BulletHoles : NetworkBehaviour, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Material material;
        [SerializeField] private int capacity = 1024;
        [SerializeField] private float diameter = 0.08f;
        [SerializeField] private float depth = 0.2f;

        private readonly NetworkList<BulletHole> holes = new();

        public string ComponentKey => "BulletHoles";
        private struct SaveData
        {
            public List<HoleData> Holes { get; set; }
        }
        private struct HoleData
        {
            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
        }
        public object SaveComponent()
        {
            var sd = new SaveData()
            {
                Holes = new List<HoleData>(holes.Count)
            };
            for (int i = 0; i < holes.Count; i++)
                sd.Holes.Add(new HoleData { Position = holes[i].Position, Normal = holes[i].Normal });
            return sd;
        }
        public void LoadComponent(JToken token)
        {
            SaveData sd = token.ToObject<SaveData>();
            holes.Clear();
            foreach (HoleData hole in sd.Holes)
                holes.Add(new BulletHole { Position = hole.Position, Normal = hole.Normal });
        }

        private readonly List<DecalProjector> projectors = new();

        private int next;

        public static BulletHoles Current { get; private set; }

        private void Awake()
        {
            Current = this;
        }

        public override void OnDestroy()
        {
            if (Current == this) Current = null;

            base.OnDestroy();
        }

        public void Add(Vector3 position, Vector3 normal)
        {
            if (!IsServer) return;

            var hole = new BulletHole { Position = position, Normal = normal };

            if (holes.Count < capacity)
            {
                holes.Add(hole);
                return;
            }

            holes[next] = hole;
            next = (next + 1) % capacity;
        }

        public override void OnNetworkSpawn()
        {
            if (material == null) Log.Warn("Bullet holes have no material, nothing will be drawn");

            holes.OnListChanged += Changed;
            Refresh();

            Log.Info($"Bullet holes are up: {holes.Count} in the world, capacity {capacity}");
        }

        public override void OnNetworkDespawn()
        {
            holes.OnListChanged -= Changed;

            foreach (DecalProjector projector in projectors)
                projector.gameObject.SetActive(false);
        }

        private void Changed(NetworkListEvent<BulletHole> change)
        {
            switch (change.Type)
            {
                case NetworkListEvent<BulletHole>.EventType.Add:
                case NetworkListEvent<BulletHole>.EventType.Insert:
                case NetworkListEvent<BulletHole>.EventType.Value:
                    Place(change.Index);
                    break;
                default:
                    Refresh();
                    break;
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < holes.Count; i++) Place(i);

            for (int i = holes.Count; i < projectors.Count; i++)
                projectors[i].gameObject.SetActive(false);
        }

        private void Place(int index)
        {
            if (material == null) return;

            BulletHole hole = holes[index];
            DecalProjector projector = Projector(index);
            int seed = Seed(hole.Position);

            Vector3 axis = Mathf.Abs(hole.Normal.y) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(-hole.Normal, axis) * Quaternion.Euler(0f, 0f, seed % 360);
            projector.transform.SetPositionAndRotation(hole.Position, rotation);

            float scale = 0.8f + ((seed >> 2) & 15) / 30f;
            projector.size = new Vector3(diameter * scale, diameter * scale, depth);
            projector.uvScale = new Vector2(0.5f, 0.5f);
            projector.uvBias = new Vector2((seed & 1) * 0.5f, ((seed >> 1) & 1) * 0.5f);

            projector.gameObject.SetActive(true);
        }

        private DecalProjector Projector(int index)
        {
            while (projectors.Count <= index)
            {
                var mount = new GameObject($"BulletHole{projectors.Count}");
                mount.transform.SetParent(transform, false);
                mount.SetActive(false);

                DecalProjector projector = mount.AddComponent<DecalProjector>();
                projector.material = material;
                projector.pivot = Vector3.zero;

                projectors.Add(projector);
            }

            return projectors[index];
        }

        private static int Seed(Vector3 position)
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
