using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Playing
{
    public static class FirstPersonCulling
    {
        private const string FirstPersonLayer = "FirstPerson";

        private static int layer = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            layer = LayerMask.NameToLayer(FirstPersonLayer);
            if (layer < 0) return;

            RenderPipelineManager.beginCameraRendering -= Strip;
            RenderPipelineManager.beginCameraRendering += Strip;
        }

        private static void Strip(ScriptableRenderContext context, Camera camera)
        {
            camera.cullingMask &= ~(1 << layer);
        }
    }
}
