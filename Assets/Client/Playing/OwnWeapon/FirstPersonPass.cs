using System;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Client.Playing
{
    [Serializable]
    public class FirstPersonPass : CustomPass
    {
        private static readonly Journal Log = Logs.Here();
        private static int culls;
        private static int shots;

        public LayerMask layer;

        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layer;
            if (culls < 3)
            {
                culls++;
                Log.Info("First person pass culls layer mask {} for camera {}", (int)layer, hdCamera.camera.name);
            }
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (shots < 3)
            {
                shots++;
                Log.Info("First person pass draws layer mask {} for camera {}", (int)layer, ctx.hdCamera.camera.name);
            }

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.Depth);
            CustomPassUtils.DrawRenderers(ctx, layer);
        }
    }
}
