using Shooter.Game.Body;
using Shooter.Game.Core;
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

        public override void OnNetworkSpawn()
        {
            if (IsOwner) Shadow(ShadowCastingMode.ShadowsOnly);
        }

        public override void OnGainedOwnership()
        {
            Shadow(ShadowCastingMode.ShadowsOnly);
        }

        public override void OnLostOwnership()
        {
            Shadow(ShadowCastingMode.On);
        }

        private void Shadow(ShadowCastingMode mode)
        {
            GameObject flesh = GetComponent<Skin>().Flesh;
            if (flesh == null)
            {
                Log.Warn($"Own player {name} has no flesh to hide");
                return;
            }

            Renderer[] renderers = flesh.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer piece in renderers) piece.shadowCastingMode = mode;

            Log.Info($"Body of the own player set to {mode}, {renderers.Length} renderers affected");
        }
    }
}
