using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    public partial class ProbeReferenceVolume
    {
        // UnloadBakingSet drops the scratch buffer pool but keeps the disk requests still in flight; the next map
        // releases them into its new pool and ReleaseScratchBuffer throws mid-render (Unity 6.6.9)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DropOrphanedStreamingRequestsOnSceneUnload()
        {
            SceneManager.sceneUnloaded += _ => instance.DropOrphanedStreamingRequests();
        }

        private void DropOrphanedStreamingRequests()
        {
            if (m_ScratchBufferPool != null) return;

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
