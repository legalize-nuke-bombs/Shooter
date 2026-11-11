using Unity.Netcode.Components;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ShooterTransformObserver))]
    public class ShooterNetworkTransform : NetworkTransform
    {
        private ShooterTransformObserver observer;

        protected override void Awake()
        {
            base.Awake();
            observer = GetComponent<ShooterTransformObserver>();
        }

        // Netcode puts a received state on the transform only in OnUpdate, which it calls for enabled ones alone, so a
        // disabled one calls it for each state itself: what the server moves while it is optimized here moves here too
        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
            if (!enabled) OnUpdate();
            observer.CheckMoved();
        }
    }
}
