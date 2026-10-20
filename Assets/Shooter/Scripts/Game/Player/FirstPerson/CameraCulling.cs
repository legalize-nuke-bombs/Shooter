using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    public static class CameraCulling
    {
        private const string FirstPersonLayerName = "FirstPerson";

        private static int culledLayerMask;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartCullingFirstPersonLayer()
        {
            culledLayerMask = LayerMask.GetMask(FirstPersonLayerName);
            if (culledLayerMask == 0) return;

            RenderPipelineManager.beginCameraRendering -= CullFirstPersonLayer;
            RenderPipelineManager.beginCameraRendering += CullFirstPersonLayer;
        }

        private static void CullFirstPersonLayer(ScriptableRenderContext context, Camera camera)
        {
            camera.cullingMask &= ~culledLayerMask;
        }
    }
}
