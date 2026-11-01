using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class ScreenResolution : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private ClientConfig clientConfig;
        private string followedResolution;

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
            if (clientConfig.ScreenResolution == followedResolution) return;

            followedResolution = clientConfig.ScreenResolution;
            if (!ScreenResolutions.GetKeysOfThisScreen().Contains(followedResolution))
            {
                followedResolution = ScreenResolutions.ToKey(ScreenResolutions.GetNative());
                Log.Info($"This screen has no resolution '{clientConfig.ScreenResolution}', the native {followedResolution} takes its place");
                clientConfig.ScreenResolution = followedResolution;
            }

            ScreenResolutions.TryParse(followedResolution, out Vector2Int size);
            Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
            Log.Info($"Screen renders at {followedResolution} in {Screen.fullScreenMode}");
        }
    }
}
