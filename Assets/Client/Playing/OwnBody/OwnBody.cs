using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Playing
{
    public class OwnBody : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject body;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            Renderer[] renderers = body.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer piece in renderers) piece.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            Log.Info("Body of the own player left as shadow only, {} renderers affected", renderers.Length);
        }
    }
}
