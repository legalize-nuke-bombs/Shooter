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
        private static int shots;

        public LayerMask layer;

        private Material probe;
        private int probePass = -1;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            Shader unlit = Shader.Find("HDRP/Unlit");
            if (unlit == null)
            {
                Log.Warn("First person pass probe found no HDRP/Unlit shader");
                return;
            }
            probe = CoreUtils.CreateEngineMaterial(unlit);
            probePass = probe.FindPass("ForwardOnly");
            Log.Info("First person pass probe material ready, forward pass index {}", probePass);
        }

        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layer;
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

            if (probe != null && probePass >= 0)
            {
                CustomPassUtils.DrawRenderers(ctx, layer, CustomPass.RenderQueueType.All, probe, probePass);
            }
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(probe);
        }
    }
}
