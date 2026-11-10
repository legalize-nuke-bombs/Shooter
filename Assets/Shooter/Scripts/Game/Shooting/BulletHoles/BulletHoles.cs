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
        private const int InitialNearbyCapacity = 1024;
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Material material;
        [SerializeField] private int projectorCount = 512;
        [SerializeField] private float diameter = 0.08f;
        [SerializeField] private float depth = 0.2f;

        private readonly List<BulletHoleData> holes = new();

        private struct SaveData
        {
            public List<BulletHoleData> Holes { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                Holes = new List<BulletHoleData>(holes)
            };
        }
        public void LoadObject(SaveToken token)
        {
            SaveData saveData = token.ToObject<SaveData>();
            holes.Clear();
            holes.AddRange(saveData.Holes);
            allHoles.SendToClients(new HoleList { Holes = holes.ToArray() });
        }

        private struct HoleList : INetworkSerializable
        {
            public BulletHoleData[] Holes;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Holes);
            }
        }

        private readonly List<BulletHole> holesInChunks = new();
        private readonly List<DecalProjector> projectors = new();
        private readonly Stack<int> freeProjectors = new();

        private Chunks chunks;
        private MessageChannel<HoleList> allHoles;
        private MessageChannel<HoleList> newHoles;
        private BulletHole[] holeOfProjector;
        private bool[] keptProjectors;
        private BulletHole[] nearbyHoles = new BulletHole[InitialNearbyCapacity];
        private float[] nearbyDistances = new float[InitialNearbyCapacity];

        private void Awake()
        {
            chunks = Registers.Instance.Get<Chunks>();
            allHoles = new MessageChannel<HoleList>(this);
            newHoles = new MessageChannel<HoleList>(this);
            allHoles.Received += HandleAllHolesReceived;
            newHoles.Received += HandleNewHolesReceived;
            holeOfProjector = new BulletHole[projectorCount];
            keptProjectors = new bool[projectorCount];
            for (int projector = 0; projector < projectorCount; projector++)
            {
                projectors.Add(CreateProjector(projector));
            }
        }

        public override void OnDestroy()
        {
            RemoveAllFromChunks();

            base.OnDestroy();
        }

        public void AddHole(Vector3 position, Vector3 normal)
        {
            if (!IsServer) return;

            BulletHoleData hole = new() { Position = position, Normal = normal };
            holes.Add(hole);
            newHoles.SendToClients(new HoleList { Holes = new[] { hole } });
        }

        public override void OnNetworkSpawn()
        {
            if (material == null) Log.Error("Bullet holes have no material, nothing will be drawn");

            RequestAllHolesRpc();
            Log.Info($"Bullet holes are up, the {projectorCount} nearest to the camera are drawn");
        }

        public void ShooterLateUpdate()
        {
            Camera eye = Camera.main;
            if (eye == null || material == null) return;

            Vector3 eyePosition = eye.transform.position;
            int nearbyCount = GatherNearbyHoles(eyePosition);
            Array.Sort(nearbyDistances, nearbyHoles, 0, nearbyCount);
            ShowNearest(Math.Min(nearbyCount, projectors.Count));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestAllHolesRpc(RpcParams rpcParams = default)
        {
            allHoles.SendToClient(rpcParams.Receive.SenderClientId, new HoleList { Holes = holes.ToArray() });
        }

        private void HandleAllHolesReceived(HoleList list)
        {
            RemoveAllFromChunks();
            foreach (BulletHoleData hole in list.Holes)
            {
                PutInChunk(hole);
            }

            Log.Info($"All {list.Holes.Length} bullet holes of the world arrived");
        }

        private void HandleNewHolesReceived(HoleList list)
        {
            foreach (BulletHoleData hole in list.Holes)
            {
                PutInChunk(hole);
            }

            Log.Debug($"{list.Holes.Length} new bullet holes arrived");
        }

        private void PutInChunk(BulletHoleData data)
        {
            BulletHole hole = new(data);
            holesInChunks.Add(hole);
            chunks.Add(Chunks.ChunkOf(data.Position), hole);
        }

        private void RemoveAllFromChunks()
        {
            foreach (BulletHole hole in holesInChunks)
            {
                chunks.Remove(Chunks.ChunkOf(hole.Data.Position), hole);
            }

            holesInChunks.Clear();
        }

        // The camera's chunk and its eight neighbours: a hole farther than a chunk away is too small to see
        private int GatherNearbyHoles(Vector3 eyePosition)
        {
            Vector2Int eyeChunk = Chunks.ChunkOf(eyePosition);
            int count = 0;
            for (int x = eyeChunk.x - 1; x <= eyeChunk.x + 1; x++)
            {
                for (int y = eyeChunk.y - 1; y <= eyeChunk.y + 1; y++)
                {
                    foreach (BulletHole hole in chunks.GetAll<BulletHole>(new Vector2Int(x, y), ChunkInactivePolicy.Include))
                    {
                        if (count == nearbyHoles.Length)
                        {
                            Array.Resize(ref nearbyHoles, count * 2);
                            Array.Resize(ref nearbyDistances, count * 2);
                        }

                        nearbyHoles[count] = hole;
                        nearbyDistances[count] = (hole.Data.Position - eyePosition).sqrMagnitude;
                        count++;
                    }
                }
            }

            return count;
        }

        private void ShowNearest(int shownCount)
        {
            Array.Clear(keptProjectors, 0, keptProjectors.Length);
            for (int rank = 0; rank < shownCount; rank++)
            {
                int projector = nearbyHoles[rank].Projector;
                if (projector != BulletHole.NoProjector) keptProjectors[projector] = true;
            }

            freeProjectors.Clear();
            for (int projector = 0; projector < projectors.Count; projector++)
            {
                if (!keptProjectors[projector]) freeProjectors.Push(projector);
            }

            for (int rank = 0; rank < shownCount; rank++)
            {
                BulletHole hole = nearbyHoles[rank];
                if (hole.Projector == BulletHole.NoProjector) ShowHole(freeProjectors.Pop(), hole);
            }

            foreach (int projector in freeProjectors)
            {
                HideProjector(projector);
            }
        }

        private void ShowHole(int projector, BulletHole hole)
        {
            BulletHole shown = holeOfProjector[projector];
            if (shown != null) shown.Projector = BulletHole.NoProjector;

            holeOfProjector[projector] = hole;
            hole.Projector = projector;
            PlaceProjector(projectors[projector], hole.Data);
        }

        private void HideProjector(int projector)
        {
            BulletHole shown = holeOfProjector[projector];
            if (shown == null) return;

            shown.Projector = BulletHole.NoProjector;
            holeOfProjector[projector] = null;
            projectors[projector].gameObject.SetActive(false);
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

        private void PlaceProjector(DecalProjector projector, BulletHoleData hole)
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
