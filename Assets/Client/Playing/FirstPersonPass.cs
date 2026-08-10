using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Client.Playing
{
    [Serializable]
    public class FirstPersonPass : CustomPass
    {
        public LayerMask layer;

        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layer;
        }

        protected override void Execute(CustomPassContext ctx)
        {
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.Depth);

            var depth = new RenderStateBlock(RenderStateMask.Depth)
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
            };
            CustomPassUtils.DrawRenderers(ctx, layer, CustomPass.RenderQueueType.All, null, 0, depth);
        }
    }
}
