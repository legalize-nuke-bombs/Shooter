using System.Collections;
using System.Text;
using Shooter.Client.Interface;
using Shooter.Configuring;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Bootstrapping
{
    internal class Session : MonoBehaviour
    {
        private const string NetworkPrefab = "NetworkManager";
        private const string OverlayPrefab = "Overlays";
        private const string CompressionPrefab = "Compression";
        private const string VhsPrefab = "VHS";
        private const string ScreenshotPrefab = "Screenshots";
        private const string MenuScene = "Menu";
        private const string BootScene = "Boot";
        private const string WorldScene = "Map";
        private const string AnyAddress = "0.0.0.0";
        private const int MenuFrameRate = 60;
        private const int UnlimitedFrameRate = -1;
        private static readonly Journal Log = Logs.Here();
        private bool ending;
        private bool loadFailed;
        private LoadingOverlay loading;

        private GameObject overlays;

        private IEnumerator Start()
        {
            Raise(CompressionPrefab);
            Raise(VhsPrefab);
            Raise(ScreenshotPrefab);
            yield return ToMenu();
        }

        private IEnumerator ToMenu()
        {
            Drop();
            Application.targetFrameRate = MenuFrameRate;

            yield return SceneManager.LoadSceneAsync(MenuScene, LoadSceneMode.Single);

            MenuScreen screen = FindAnyObjectByType<MenuScreen>();
            if (screen == null)
            {
                Log.Error($"Scene {MenuScene} carries no menu screen, there is nothing to press");
                yield break;
            }

            screen.Hosting += Host;
            screen.Joining += Join;
            screen.Quitting += Quit;

            if (loadFailed)
            {
                loadFailed = false;
                screen.Warn("Не удалось загрузить сохранение", "Файл повреждён или несовместим с этой версией");
            }

            ending = false;
            Log.Info("Menu is up");
        }

        private void Host(string save)
        {
            StartCoroutine(Begin(true, save));
        }

        private void Join()
        {
            StartCoroutine(Begin(false, null));
        }

        private void Quit()
        {
            Log.Info("Leaving the game by the player's own hand");
            Application.Quit();
        }

        private IEnumerator Begin(bool hosting, string save)
        {
            Application.targetFrameRate = UnlimitedFrameRate;
            yield return SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);

            Overlays();

            if (hosting)
            {
                loading.Show(LoadingStage.Scene);
                yield return SceneManager.LoadSceneAsync(WorldScene, LoadSceneMode.Single);
            }

            NetworkManager network = Network();
            if (network == null)
            {
                yield return ToMenu();
                yield break;
            }

            ClientConfig client = Config.Read().Client;

            Address(network, hosting);
            network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(client.Name);
            network.OnServerStopped += Stopped;
            network.OnClientStopped += Stopped;

            loading.Show(hosting ? LoadingStage.Server : LoadingStage.Connection);

            if (!(hosting ? network.StartHost() : network.StartClient()))
            {
                Log.Error($"The {(hosting ? "host" : "client")} refused to start");
                yield return ToMenu();
                yield break;
            }

            Log.Info($"{(hosting ? "Host" : "Client")} is up as {client.Name}");

            if (!hosting)
            {
                network.SceneManager.OnSceneEvent += Synchronizing;
                network.OnClientConnectedCallback += Entered;
                yield break;
            }

            if (save != null) Restore(network, save);
            if (network.ShutdownInProgress) yield break;

            loading.Hide();
        }

        private void Restore(NetworkManager network, string save)
        {
            SaveManager saves = SaveManager.Current;
            if (saves == null)
            {
                Log.Error($"World has no save manager, {save} stays unloaded, shutting the host down");
                loadFailed = true;
                network.Shutdown();
                return;
            }

            Log.Info($"Loading the world from {save}");
            loading.Show(LoadingStage.Save);
            FrozenWorld world = saves.Freeze();
            if (saves.Load(world, save)) return;

            Log.Warn($"The world failed to load {save}, shutting the host down");
            loadFailed = true;
            network.Shutdown();
        }

        private void Synchronizing(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    loading.Show(LoadingStage.Scene);
                    break;
                case SceneEventType.LoadComplete:
                    loading.Show(LoadingStage.Synchronization);
                    break;
            }
        }

        private void Entered(ulong ignored)
        {
            loading.Hide();
        }

        private void Stopped(bool ignored)
        {
            if (ending) return;

            ending = true;
            StartCoroutine(End());
        }

        private IEnumerator End()
        {
            Log.Info("The session is over, heading back to the menu");

            yield return null;
            yield return ToMenu();
        }

        private void Drop()
        {
            if (overlays != null)
            {
                Destroy(overlays);
                overlays = null;
                loading = null;
            }

            if (NetworkManager.Singleton != null) Destroy(NetworkManager.Singleton.gameObject);
        }

        private void Address(NetworkManager network, bool hosting)
        {
            UnityTransport transport = network.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Log.Warn("No unity transport to configure");
                return;
            }

            transport.UseEncryption = true;

            if (hosting)
            {
                ServerConfig server = Config.Read().Server;
                transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);
                transport.SetServerSecrets(WorldCert, WorldKey);
                Log.Info($"World listens on port {server.Port}, channel encrypted");
                return;
            }

            ClientConfig client = Config.Read().Client;
            transport.SetConnectionData(client.Address, client.Port);
            transport.SetClientSecrets(WorldCommonName, WorldCert);
            Log.Info($"Heading for {client.Address}:{client.Port}, channel encrypted");
        }

        private void Raise(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                Log.Error($"No {prefabName} prefab in Resources, that service stays down");
                return;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = prefabName;
            DontDestroyOnLoad(instance);
            Log.Info($"{prefabName} is up");
        }

        private void Overlays()
        {
            GameObject prefab = Resources.Load<GameObject>(OverlayPrefab);

            overlays = Instantiate(prefab);
            overlays.name = OverlayPrefab;
            DontDestroyOnLoad(overlays);
            loading = overlays.GetComponentInChildren<LoadingOverlay>();
            Log.Info("Overlays are up");
        }

        private NetworkManager Network()
        {
            if (NetworkManager.Singleton != null) return NetworkManager.Singleton;

            GameObject prefab = Resources.Load<GameObject>(NetworkPrefab);
            if (prefab == null)
            {
                Log.Error($"No {NetworkPrefab} prefab in Resources, refusing to start");
                return null;
            }

            GameObject instance = Instantiate(prefab);
            instance.name = NetworkPrefab;
            DontDestroyOnLoad(instance);

            NetworkManager network = instance.GetComponent<NetworkManager>();
            if (network == null) Log.Error($"Prefab {NetworkPrefab} has no NetworkManager component");

            return network;
        }

        // TEMP: одноразовый self-signed серт для smoke-test DTLS. Убрать при переходе на генерацию серта мира.
        private const string WorldCommonName = "shooter-world";

        private const string WorldCert = @"-----BEGIN CERTIFICATE-----
MIIDETCCAfmgAwIBAgIUJVqA8vnKMVXxOVzYO+2d1Uu3+gcwDQYJKoZIhvcNAQEL
BQAwGDEWMBQGA1UEAwwNc2hvb3Rlci13b3JsZDAeFw0yNjA4MjUxMjI1NDZaFw0z
NjA4MjIxMjI1NDZaMBgxFjAUBgNVBAMMDXNob290ZXItd29ybGQwggEiMA0GCSqG
SIb3DQEBAQUAA4IBDwAwggEKAoIBAQC0X6RXU5SMcS5mS1NVc7loXQ22ydwff3Yf
jdefJ7hsmM3o5XOW0sctX2HmE+191Kutw49SHzFhNWTY8d3Tlt7zcqQSKGCLU9UA
iL1yQ3hXXVa7MiBigERl67YnsXJdOFdf96sCpZzyprd1pepsNbUlUSkVvcalawiL
Qzak69tv3f77D8sDP6Ch4jyeQfVB6gCwwhy1AbLxc21gEPw4VtYtwn6KYdm6hbNr
Q0DOteD9p3KLPuvYtVVNlu2uGjz1MSgpEImVvtPV/TorMaqRIEn57k5TSoHsnNVs
hTxkgfbmgE9oL2IpGcgCTU6WUyh3+5zuSfpl5psxnuYPfqZr1+XNAgMBAAGjUzBR
MB0GA1UdDgQWBBTVipY2TwVGO6C3DVqrID4ukBlYrTAfBgNVHSMEGDAWgBTVipY2
TwVGO6C3DVqrID4ukBlYrTAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUA
A4IBAQAm5EBiBeCYgciMsnJYz4WexIWa0nI7BYIlaD8GXYJqs56wPHOO/pOeN/+t
kr1tlV00teWvn+CnDBICV50zQDiqMQ5PZLqaYEw87mIHSuS8dgXWAJLK+tWM2Sxw
hzIdmuZw11f9oG546yXEsqECU2jkWxsTkG5U0LtKlKPVYo1pn+Lr/EGl+2gXYNqz
VHqFXDuCJh8xdKEaDAF9mSvMh60sGbKkvDov7pnIRyo+TnqvsQPFcq3aGqNqF/BG
6rTQjrfWQzyHTC92TpjzhiSc8z2m2H2JRn2eNVudBrcIBwJFqDxlx1oGTp961rKY
WboWi8GanF7YUbYrYYPz1/F4vFBW
-----END CERTIFICATE-----";

        private const string WorldKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC0X6RXU5SMcS5m
S1NVc7loXQ22ydwff3YfjdefJ7hsmM3o5XOW0sctX2HmE+191Kutw49SHzFhNWTY
8d3Tlt7zcqQSKGCLU9UAiL1yQ3hXXVa7MiBigERl67YnsXJdOFdf96sCpZzyprd1
pepsNbUlUSkVvcalawiLQzak69tv3f77D8sDP6Ch4jyeQfVB6gCwwhy1AbLxc21g
EPw4VtYtwn6KYdm6hbNrQ0DOteD9p3KLPuvYtVVNlu2uGjz1MSgpEImVvtPV/Tor
MaqRIEn57k5TSoHsnNVshTxkgfbmgE9oL2IpGcgCTU6WUyh3+5zuSfpl5psxnuYP
fqZr1+XNAgMBAAECggEAT+DXJ7Ek5PkugjCzi/E+15/19c/5Qp2w0xJ+vcXaX4Vg
EtaiVNWtTUOjWD/U+deX29DyBH054gHCUmzyPsTeoVNQo5XsA2exuZXUx+hnP9Ff
GnF9dAG3yKcVOQjVS8EquJ42xmpPUgpQzrIWWauDOC50EmPDt/fphbrTVT/6ItSe
jTDjEG/z67lrrlX3arI59rGfw7svFhlej2h9yllrdOKtSDnxQgnLGgVMRGgQPJLM
35Wp9eHyrjTaLXhH4ujXz8AjeA95//alFs9/P8Z0IxdK2P2Y56JIuU+TeCkKzh14
OYS/wYwVqwDG0xHFTnjjHXhOu8BdMljklUncw4V85QKBgQDwqnV+RXC9EfTNi7Lg
X6jblBBJVw1+BcfZd7ZNn2DlKBxS3/xeuwVucFj/BQf1k8H/Mfhmc3B2+4qJ0Rt3
TbOQP7HdwkIYsssofOLwm8aKyaKtextI0j8U7e1MnWmimYtZ+gdFLx5ezcw39/oW
CrJ2HFb/M/UMJu3rpGHWS961swKBgQC/3b7295d5jOYk/ZGrDq2Y3Yr3hz5ardV4
Qc2qXXJCtzcZp6Iyd2zjro07hNuzm+Zfb/CNAMAYj75lIW4ce4N2KKYvxYZTyGHF
Ng9gWz+oAxzRZjoyYTGjEpUtf2oWcL+XVRR549XBA2eo/SD1/Lc1tMRTzCKuW373
T9ygBdg2fwKBgEUCG76hWrpcM73cmOYNh/Wudx0QgSXpsmyBDx0i3j3XSofZAhyH
s/7+6AX4A1g/jhkG0xtNbqovZoIuG5oSBbMPEIlt8lXyrp5lcQ1dHYkeWC82ZZRz
9PKjZq/ZUzj0niimsP79i8/TYwOJb4RyfMmxRqDW3SUm5IH1GLjB+JJRAoGBAIKX
bMZuSXSbOX6N9NsoN3JnwJGwRPm1finHKDRAPGg6ik908QpGjR//i/Op/1wlzczB
xUpD63wMQrxU37yVOSpwioTTfhWCu0FfBWJBWXeC/tdsLEpkK0PifxUjt1Kk1VMs
vq4kLDaemazE9e1YYF82tbaPqD9i2W19tx5YPA0DAoGAeiSUkbW15fwa0QW9hvE3
09rqVNpQDCjZTTeaY9BGDH1oyRYXRS+AAKGF51uM3WMvYUHfjnbMeWB01EUWKqJB
8qSpvJeVFD9+4xevWPksc3QOTcGIusI8T52OyDmCEbYFmlWtggPGjEXrar+nolxI
lhUilGxCow5VVQGP4uStZGA=
-----END PRIVATE KEY-----";
    }
}
