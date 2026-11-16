using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace Shooter.Game
{
    [Serializable]
    public class FirstPersonPrepass : CustomPass
    {
        private const string WriteNormalBufferKeyword = "WRITE_NORMAL_BUFFER";
        private static readonly ShaderTagId[] DepthPasses = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        private static readonly ShaderTagId[] MotionVectorsPass = { HDShaderPassNames.s_MotionVectorsName };

        public LayerMask layer;

        private readonly RenderTargetIdentifier[] motionVectorsAndNormals = new RenderTargetIdentifier[2];

        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)layer;
        }

        // SSAO and the temporal passes read these buffers before FirstPersonPass draws the colour over the world they were
        // made of. Unity draws the motion vectors pass only for renderers that moved this frame; a still weapon means a still
        // camera, and the camera's own motion vectors are zero then
        protected override void Execute(CustomPassContext ctx)
        {
            CoreUtils.SetKeyword(ctx.cmd, WriteNormalBufferKeyword, true);

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraNormalBuffer, ctx.cameraDepthBuffer);
            DrawOpaque(ctx, DepthPasses, PerObjectData.None);

            motionVectorsAndNormals[0] = ctx.cameraMotionVectorsBuffer;
            motionVectorsAndNormals[1] = ctx.cameraNormalBuffer;
            CoreUtils.SetRenderTarget(ctx.cmd, motionVectorsAndNormals, ctx.cameraDepthBuffer);
            DrawOpaque(ctx, MotionVectorsPass, PerObjectData.MotionVectors);

            CoreUtils.SetKeyword(ctx.cmd, WriteNormalBufferKeyword, ctx.hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward);
        }

        private void DrawOpaque(CustomPassContext ctx, ShaderTagId[] passes, PerObjectData perObjectData)
        {
            RendererListDesc renderers = new(passes, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = perObjectData,
                renderQueueRange = RenderQueueRange.opaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                layerMask = layer
            };
            CoreUtils.DrawRendererList(ctx.cmd, ctx.renderContext.CreateRendererList(renderers));
        }
    }
}
