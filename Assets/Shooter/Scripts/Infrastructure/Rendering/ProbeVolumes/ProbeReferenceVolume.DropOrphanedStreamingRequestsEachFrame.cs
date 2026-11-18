namespace UnityEngine.Rendering
{
    public partial class ProbeReferenceVolume
    {
        // UnloadBakingSet drops the scratch buffer pool but keeps the disk requests still in flight (Unity 6.6.9):
        // the next map releases them into its new pool mid-render, a pipeline Dispose throws in CleanupStreaming
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DropOrphanedStreamingRequestsEachFrame()
        {
            RenderPipelineManager.beginContextRendering -= DropOrphanedStreamingRequestsBeforeRendering;
            RenderPipelineManager.beginContextRendering += DropOrphanedStreamingRequestsBeforeRendering;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void DropOrphanedStreamingRequestsEachEditorTick()
        {
            DropOrphanedStreamingRequestsEachFrame();
            UnityEditor.EditorApplication.update += () => instance.DropOrphanedStreamingRequests();
        }
#endif

        private static void DropOrphanedStreamingRequestsBeforeRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
        {
            instance.DropOrphanedStreamingRequests();
        }

        private void DropOrphanedStreamingRequests()
        {
            if (m_ScratchBufferPool != null || m_ActiveStreamingRequests.Count == 0) return;

            foreach (CellStreamingRequest request in m_ActiveStreamingRequests)
            {
                CellStreamingScratchBuffer scratchBuffer = request.scratchBuffer;
                request.Clear();
                scratchBuffer?.Dispose();
                m_StreamingRequestsPool.Release(request);
            }
            m_ActiveStreamingRequests.Clear();
        }
    }
}
