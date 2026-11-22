using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class WaterSplashes : NetworkBehaviour, IShooterUpdateable
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly int FrameProperty = Shader.PropertyToID("_BaseColorMap_ST");

        [SerializeField] private Material material;
        [SerializeField] private int splashCount = 16;
        [SerializeField] private int columns = 5;
        [SerializeField] private int rows = 5;
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private float size = 1f;

        private Splash[] splashes;
        private Transform holder;
        private Mesh mesh;
        private MaterialPropertyBlock properties;
        private int nextSplashToReuse;

        private struct Splash
        {
            public MeshRenderer Renderer;
            public float StartTime;
            public bool Playing;
        }

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            mesh = CreateQuadMesh();
            holder = new GameObject("Water Splashes").transform;
            splashes = new Splash[Mathf.Max(splashCount, 1)];
            for (int i = 0; i < splashes.Length; i++)
            {
                splashes[i].Renderer = CreateQuad(i);
            }
        }

        public override void OnDestroy()
        {
            if (holder != null) Destroy(holder.gameObject);
            if (mesh != null) Destroy(mesh);

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (material == null) Log.Error("Water splashes have no material, nothing will be drawn");
        }

        public void LeaveSplash(Vector3 position)
        {
            if (!IsServer) return;

            LeaveSplashRpc(position);
        }

        public void ShooterUpdate()
        {
            Camera eye = Camera.main;

            for (int i = 0; i < splashes.Length; i++)
            {
                if (!splashes[i].Playing) continue;

                float progress = (Time.time - splashes[i].StartTime) / duration;
                if (progress >= 1f)
                {
                    splashes[i].Playing = false;
                    splashes[i].Renderer.gameObject.SetActive(false);
                    continue;
                }

                ShowFrame(splashes[i].Renderer, (int)(progress * columns * rows));
                if (eye != null) FaceAround(splashes[i].Renderer.transform, eye.transform.position);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void LeaveSplashRpc(Vector3 position)
        {
            if (material == null) return;

            int i = TakeSplash();
            splashes[i].StartTime = Time.time;
            splashes[i].Playing = true;

            MeshRenderer quad = splashes[i].Renderer;
            quad.transform.position = position;
            ShowFrame(quad, 0);
            Camera eye = Camera.main;
            if (eye != null) FaceAround(quad.transform, eye.transform.position);
            quad.gameObject.SetActive(true);
        }

        private void ShowFrame(MeshRenderer quad, int frame)
        {
            float width = 1f / columns, height = 1f / rows;
            properties.SetVector(FrameProperty, new Vector4(width, height, frame % columns * width, 1f - (frame / columns + 1) * height));
            quad.SetPropertyBlock(properties);
        }

        private int TakeSplash()
        {
            for (int i = 0; i < splashes.Length; i++)
            {
                if (!splashes[i].Playing) return i;
            }

            int reused = nextSplashToReuse;
            nextSplashToReuse = (nextSplashToReuse + 1) % splashes.Length;
            return reused;
        }

        private static void FaceAround(Transform quad, Vector3 eyePosition)
        {
            Vector3 toEye = eyePosition - quad.position;
            toEye.y = 0f;
            if (toEye.sqrMagnitude > 0.0001f) quad.rotation = Quaternion.LookRotation(-toEye);
        }

        private MeshRenderer CreateQuad(int index)
        {
            GameObject quad = new($"Splash {index}");
            quad.transform.SetParent(holder, false);
            quad.transform.localScale = Vector3.one * size;
            quad.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer quadRenderer = quad.AddComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = material;
            quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
            quad.SetActive(false);
            return quadRenderer;
        }

        private static Mesh CreateQuadMesh()
        {
            Mesh quad = new() { name = "Water Splash Quad" };
            quad.vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, 1f, 0f), new Vector3(-0.5f, 1f, 0f) };
            quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            quad.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            quad.tangents = new[] { new Vector4(1f, 0f, 0f, -1f), new Vector4(1f, 0f, 0f, -1f), new Vector4(1f, 0f, 0f, -1f), new Vector4(1f, 0f, 0f, -1f) };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.RecalculateBounds();
            return quad;
        }
    }
}
