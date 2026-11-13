using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
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
            if (IsPathTraced(ctx.hdCamera)) return;

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.Depth);

            RenderStateBlock depthTestState = new(RenderStateMask.Depth)
            {
                depthState = new DepthState(true, CompareFunction.LessEqual)
            };
            CustomPassUtils.DrawRenderers(ctx, layer, RenderQueueType.All, null, 0, depthTestState);
        }

        // A path traced frame has no light lists for raster passes: HDRP skips its own, and the weapon is traced like the rest of the scene
        private static bool IsPathTraced(HDCamera hdCamera)
        {
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && hdCamera.volumeStack.GetComponent<PathTracing>().enable.value;
        }
    }
}
