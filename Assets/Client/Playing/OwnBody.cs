using Shooter.Game.Body;
using Shooter.Game.Llm;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using Shooter.Game.Core;

namespace Shooter.Client.Playing
{
    public class OwnBody : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            GameObject flesh = this.Find<Skin>().Flesh;
            if (flesh == null)
            {
                Log.Warn($"Own player {this.NameOf()} has no flesh to hide");
                return;
            }

            Renderer[] renderers = flesh.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer piece in renderers) piece.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            Log.Info($"Body of the own player left as shadow only, {renderers.Length} renderers affected");
        }
    }
}
