using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class WaterSplashes : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly int RadiusProperty = Shader.PropertyToID("Splash Radius");
        private static readonly int VelocityProperty = Shader.PropertyToID("Velocity");

        [SerializeField] private VisualEffectAsset splash;
        [SerializeField] private int splashCount = 16;
        [SerializeField] private float radius = 0.25f;
        [SerializeField] private float speed = 5f;

        private VisualEffect[] splashes;
        private Transform holder;
        private int nextSplash;

        private void Awake()
        {
            holder = new GameObject("Water Splashes").transform;
            splashes = new VisualEffect[Mathf.Max(splashCount, 1)];
        }

        public override void OnDestroy()
        {
            if (holder != null) Destroy(holder.gameObject);

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (splash == null) Log.Error("Water splashes have no visual effect, nothing will be drawn");
        }

        public void LeaveSplash(Vector3 position, Vector3 direction)
        {
            if (!IsServer) return;

            LeaveSplashRpc(position, direction);
        }

        [Rpc(SendTo.Everyone)]
        private void LeaveSplashRpc(Vector3 position, Vector3 direction)
        {
            if (splash == null) return;

            if (splashes[nextSplash] == null) splashes[nextSplash] = CreateSplash(nextSplash);
            VisualEffect effect = splashes[nextSplash];
            nextSplash = (nextSplash + 1) % splashes.Length;

            effect.transform.position = position;
            effect.SetFloat(RadiusProperty, radius);
            effect.SetVector3(VelocityProperty, direction.normalized * speed);
            effect.Play();
        }

        private VisualEffect CreateSplash(int index)
        {
            GameObject splashObject = new($"Splash {index}");
            splashObject.transform.SetParent(holder, false);
            VisualEffect effect = splashObject.AddComponent<VisualEffect>();
            effect.visualEffectAsset = splash;
            effect.initialEventName = "";
            return effect;
        }
    }
}
