using System.Collections.Generic;
using Shooter.Game.Body;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Skin))]
    public class OwnBody : NetworkBehaviour
    {
        private const string ReflectionOnlyLayer = "ReflectionOnly";
        private const string ReflectionName = "Reflection";

        private static readonly Journal Log = Logs.Here();

        private readonly List<GameObject> reflections = new();

        private Skin skin;
        private Inventory inventory;
        private GameObject flesh;
        private int reflectionLayer = -1;
        private bool owning;
        private bool dirty;

        private void Awake()
        {
            skin = GetComponent<Skin>();
            inventory = GetComponent<Inventory>();
            reflectionLayer = LayerMask.NameToLayer(ReflectionOnlyLayer);
            enabled = false;
        }

        private void LateUpdate()
        {
            if (dirty || flesh != skin.Flesh) Rebuild();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner) Own();
        }

        public override void OnGainedOwnership()
        {
            if (IsOwner) Own();
        }

        public override void OnLostOwnership()
        {
            Disown();
        }

        public override void OnNetworkDespawn()
        {
            Disown();
        }

        private void Own()
        {
            if (owning) return;
            owning = true;
            dirty = true;
            enabled = true;

            if (inventory != null) inventory.Changed += Invalidate;

            Log.Info($"Own player {name} hides its body and mirrors it on the {ReflectionOnlyLayer} layer");
        }

        private void Disown()
        {
            if (!owning) return;
            owning = false;
            enabled = false;

            if (inventory != null) inventory.Changed -= Invalidate;

            Clear();
            Shadow(ShadowCastingMode.On);
            flesh = null;
        }

        private void Invalidate()
        {
            dirty = true;
        }

        private void Rebuild()
        {
            dirty = false;
            flesh = skin.Flesh;
            Clear();

            if (flesh == null)
            {
                Log.Warn($"Own player {name} has no flesh to hide");
                return;
            }

            foreach (Renderer piece in flesh.GetComponentsInChildren<Renderer>(true))
            {
                if (piece.gameObject.layer == reflectionLayer) continue;

                piece.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

                GameObject mirror = Mirror(piece);
                if (mirror != null) reflections.Add(mirror);
            }

            Log.Info($"Own player {name} rebuilt its reflection from {reflections.Count} pieces");
        }

        private GameObject Mirror(Renderer piece)
        {
            if (reflectionLayer < 0) return null;

            var mirror = new GameObject(ReflectionName);
            mirror.layer = reflectionLayer;
            mirror.transform.SetParent(piece.transform, false);

            if (piece is SkinnedMeshRenderer skinned)
            {
                var clone = mirror.AddComponent<SkinnedMeshRenderer>();
                clone.sharedMesh = skinned.sharedMesh;
                clone.sharedMaterials = skinned.sharedMaterials;
                clone.bones = skinned.bones;
                clone.rootBone = skinned.rootBone;
                clone.localBounds = skinned.localBounds;
                clone.quality = skinned.quality;
                clone.updateWhenOffscreen = skinned.updateWhenOffscreen;
                clone.shadowCastingMode = ShadowCastingMode.Off;
                return mirror;
            }

            MeshFilter filter = piece.GetComponent<MeshFilter>();
            if (piece is MeshRenderer flat && filter != null)
            {
                mirror.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var clone = mirror.AddComponent<MeshRenderer>();
                clone.sharedMaterials = flat.sharedMaterials;
                clone.shadowCastingMode = ShadowCastingMode.Off;
                return mirror;
            }

            Destroy(mirror);
            return null;
        }

        private void Clear()
        {
            foreach (GameObject mirror in reflections)
                if (mirror != null)
                    Destroy(mirror);

            reflections.Clear();
        }

        private void Shadow(ShadowCastingMode mode)
        {
            GameObject body = skin.Flesh;
            if (body == null) return;

            foreach (Renderer piece in body.GetComponentsInChildren<Renderer>(true))
                if (piece.gameObject.layer != reflectionLayer)
                    piece.shadowCastingMode = mode;
        }
    }
}
