using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class VerticalSync : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private ClientConfig clientConfig;
        private string followedVerticalSync;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            clientConfig = Config.GetGameConfig().Client;
            clientConfig.propertyChanged += HandleClientConfigChanged;
            FollowClientConfig();
        }

        private void OnDisable()
        {
            clientConfig.propertyChanged -= HandleClientConfigChanged;
        }

        private void HandleClientConfigChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            FollowClientConfig();
        }

        private void FollowClientConfig()
        {
            if (clientConfig.VerticalSync == followedVerticalSync) return;

            followedVerticalSync = clientConfig.VerticalSync;
            QualitySettings.vSyncCount = followedVerticalSync == Toggles.On ? 1 : 0;
            Log.Info($"Vertical sync {followedVerticalSync}");
        }
    }
}
