using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    [RequireComponent(typeof(Skin))]
    public class OwnBodyHider : NetworkBehaviour
    {
        private const string ReflectionOnlyLayerName = "ReflectionOnly";
        private const string ReflectionCloneName = "Reflection";

        private static readonly Journal Log = Logs.Here();

        private readonly List<GameObject> reflectionClones = new();

        private Skin skin;
        private InventoryHolder inventoryHolder;
        private WeaponView weaponView;
        private GameObject hiddenModelInstance;
        private int reflectionOnlyLayer = -1;
        private bool hiding;
        private bool needsRebuild;

        private void Awake()
        {
            skin = GetComponent<Skin>();
            inventoryHolder = GetComponent<InventoryHolder>();
            weaponView = GetComponent<WeaponView>();
            reflectionOnlyLayer = LayerMask.NameToLayer(ReflectionOnlyLayerName);
            enabled = false;
        }

        private void LateUpdate()
        {
            if (needsRebuild || hiddenModelInstance != skin.ModelInstance) HideModelAndRebuildReflection();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner) StartHidingOwnBody();
        }

        public override void OnGainedOwnership()
        {
            if (IsOwner) StartHidingOwnBody();
        }

        public override void OnLostOwnership()
        {
            StopHidingOwnBody();
        }

        public override void OnNetworkDespawn()
        {
            StopHidingOwnBody();
        }

        private void StartHidingOwnBody()
        {
            if (hiding) return;
            hiding = true;
            needsRebuild = true;
            enabled = true;

            if (inventoryHolder != null) inventoryHolder.Changed += MarkRebuildNeeded;

            Log.Info($"Own player {name} hides its body and mirrors it on the {ReflectionOnlyLayerName} layer");
        }

        private void StopHidingOwnBody()
        {
            if (!hiding) return;
            hiding = false;
            enabled = false;

            if (inventoryHolder != null) inventoryHolder.Changed -= MarkRebuildNeeded;

            DestroyReflectionClones();
            SetShadowCastingMode(ShadowCastingMode.On);
            hiddenModelInstance = null;
        }

        private void MarkRebuildNeeded()
        {
            needsRebuild = true;
        }

        private void HideModelAndRebuildReflection()
        {
            needsRebuild = false;
            hiddenModelInstance = skin.ModelInstance;
            DestroyReflectionClones();

            if (hiddenModelInstance == null)
            {
                Log.Warn($"Own player {name} has no model to hide");
                return;
            }

            foreach (Renderer piece in GetModelAndHeldWeaponRenderers(hiddenModelInstance))
            {
                if (piece.gameObject.layer == reflectionOnlyLayer) continue;

                piece.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

                GameObject reflectionClone = CreateReflectionClone(piece);
                if (reflectionClone != null) reflectionClones.Add(reflectionClone);
            }

            Log.Info($"Own player {name} rebuilt its reflection from {reflectionClones.Count} pieces");
        }

        private GameObject CreateReflectionClone(Renderer piece)
        {
            if (reflectionOnlyLayer < 0) return null;

            GameObject reflectionClone = new(ReflectionCloneName);
            reflectionClone.layer = reflectionOnlyLayer;
            reflectionClone.transform.SetParent(piece.transform, false);

            if (piece is SkinnedMeshRenderer skinnedPiece)
            {
                SkinnedMeshRenderer cloneRenderer = reflectionClone.AddComponent<SkinnedMeshRenderer>();
                cloneRenderer.sharedMesh = skinnedPiece.sharedMesh;
                cloneRenderer.sharedMaterials = skinnedPiece.sharedMaterials;
                cloneRenderer.bones = skinnedPiece.bones;
                cloneRenderer.rootBone = skinnedPiece.rootBone;
                cloneRenderer.localBounds = skinnedPiece.localBounds;
                cloneRenderer.quality = skinnedPiece.quality;
                cloneRenderer.updateWhenOffscreen = skinnedPiece.updateWhenOffscreen;
                cloneRenderer.shadowCastingMode = ShadowCastingMode.Off;
                return reflectionClone;
            }

            MeshFilter meshFilter = piece.GetComponent<MeshFilter>();
            if (piece is MeshRenderer meshPiece && meshFilter != null)
            {
                reflectionClone.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;
                MeshRenderer cloneRenderer = reflectionClone.AddComponent<MeshRenderer>();
                cloneRenderer.sharedMaterials = meshPiece.sharedMaterials;
                cloneRenderer.shadowCastingMode = ShadowCastingMode.Off;
                return reflectionClone;
            }

            Destroy(reflectionClone);
            return null;
        }

        private void DestroyReflectionClones()
        {
            foreach (GameObject reflectionClone in reflectionClones)
            {
                if (reflectionClone != null) Destroy(reflectionClone);
            }

            reflectionClones.Clear();
        }

        private void SetShadowCastingMode(ShadowCastingMode mode)
        {
            foreach (Renderer piece in GetModelAndHeldWeaponRenderers(skin.ModelInstance))
            {
                if (piece.gameObject.layer != reflectionOnlyLayer) piece.shadowCastingMode = mode;
            }
        }

        private IEnumerable<Renderer> GetModelAndHeldWeaponRenderers(GameObject model)
        {
            if (model != null)
            {
                foreach (Renderer piece in model.GetComponentsInChildren<Renderer>(true))
                {
                    yield return piece;
                }
            }

            GameObject heldWeapon = weaponView == null ? null : weaponView.ShownWeapon;
            if (heldWeapon == null) yield break;

            foreach (Renderer piece in heldWeapon.GetComponentsInChildren<Renderer>(true))
            {
                yield return piece;
            }
        }
    }
}
