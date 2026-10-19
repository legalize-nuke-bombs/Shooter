using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    public static class CameraCulling
    {
        private const string FirstPersonLayerName = "FirstPerson";
        private const string ReflectionOnlyLayerName = "ReflectionOnly";

        private static int culledLayerMask;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartCullingFirstPersonAndReflectionOnlyLayers()
        {
            culledLayerMask = LayerMask.GetMask(FirstPersonLayerName, ReflectionOnlyLayerName);
            if (culledLayerMask == 0) return;

            RenderPipelineManager.beginCameraRendering -= CullFirstPersonAndReflectionOnlyLayers;
            RenderPipelineManager.beginCameraRendering += CullFirstPersonAndReflectionOnlyLayers;
        }

        private static void CullFirstPersonAndReflectionOnlyLayers(ScriptableRenderContext context, Camera camera)
        {
            camera.cullingMask &= ~culledLayerMask;
        }
    }
}
