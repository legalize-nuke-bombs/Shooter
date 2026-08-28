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
        private static readonly Journal Log = Logs.Here();

        private readonly List<Renderer> pieces = new();
        private readonly List<ShadowCastingMode> modes = new();

        private Skin skin;
        private Inventory inventory;
        private Camera eye;
        private GameObject flesh;
        private bool watching;
        private bool dirty;

        private void Awake()
        {
            skin = GetComponent<Skin>();
            inventory = GetComponent<Inventory>();
            eye = GetComponentInChildren<Camera>(true);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner) Watch();
        }

        public override void OnGainedOwnership()
        {
            if (IsOwner) Watch();
        }

        public override void OnLostOwnership()
        {
            Drop();
        }

        public override void OnNetworkDespawn()
        {
            Drop();
        }

        public override void OnDestroy()
        {
            Drop();
            base.OnDestroy();
        }

        private void Watch()
        {
            if (watching) return;
            watching = true;
            dirty = true;

            if (inventory != null) inventory.Changed += Invalidate;
            RenderPipelineManager.beginCameraRendering += Hide;
            RenderPipelineManager.endCameraRendering += Show;

            Log.Info($"Own player {name} hides its body from the eye camera only");
        }

        private void Drop()
        {
            if (!watching) return;
            watching = false;

            if (inventory != null) inventory.Changed -= Invalidate;
            RenderPipelineManager.beginCameraRendering -= Hide;
            RenderPipelineManager.endCameraRendering -= Show;

            Restore();
            pieces.Clear();
            modes.Clear();
            flesh = null;
        }

        private void Invalidate()
        {
            dirty = true;
        }

        private void Hide(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != eye) return;

            if (dirty || flesh != skin.Flesh) Rebuild();

            foreach (Renderer piece in pieces)
                if (piece != null)
                    piece.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        private void Show(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != eye) return;

            Restore();
        }

        private void Restore()
        {
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i] != null)
                    pieces[i].shadowCastingMode = modes[i];
        }

        private void Rebuild()
        {
            dirty = false;
            flesh = skin.Flesh;
            pieces.Clear();
            modes.Clear();

            if (flesh == null)
            {
                Log.Warn($"Own player {name} has no flesh to hide");
                return;
            }

            foreach (Renderer piece in flesh.GetComponentsInChildren<Renderer>(true))
            {
                pieces.Add(piece);
                modes.Add(piece.shadowCastingMode);
            }
        }
    }
}
