using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Playing
{
    public static class CameraCulling
    {
        private const string FirstPersonLayer = "FirstPerson";
        private const string ReflectionOnlyLayer = "ReflectionOnly";

        private static int hidden;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            hidden = LayerMask.GetMask(FirstPersonLayer, ReflectionOnlyLayer);
            if (hidden == 0) return;

            RenderPipelineManager.beginCameraRendering -= Strip;
            RenderPipelineManager.beginCameraRendering += Strip;
        }

        private static void Strip(ScriptableRenderContext context, Camera camera)
        {
            camera.cullingMask &= ~hidden;
        }
    }
}
