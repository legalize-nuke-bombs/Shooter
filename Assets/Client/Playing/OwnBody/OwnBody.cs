using Shooter.Game.Body;
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
            if (!IsOwner) return;

            GameObject flesh = GetComponent<Skin>().Flesh;
            if (flesh == null)
            {
                Log.Warn("Own player {} has no flesh to hide", name);
                return;
            }

            Renderer[] renderers = flesh.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer piece in renderers) piece.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            Log.Info("Body of the own player left as shadow only, {} renderers affected", renderers.Length);
        }
    }
}
