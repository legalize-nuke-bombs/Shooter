using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    // Starts this machine from the menu. A host spawns the game state and loads the world at once; a client stays
    // in the menu until the server takes it to the world, and hears here if it never gets there
    public class Launcher : MonoBehaviour
    {
        private const string AnyAddress = "0.0.0.0";
        private const string BadInvite = "Неверный код подключения";
        private const string BadInviteDetails = "Попросите у хоста код ещё раз и вставьте его целиком";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private GameObject state;
        [SerializeField] private Launch launch;
        [SerializeField] private string world = "Map";

        private NetworkManager network;

        public event Action<Refusal> Refused;

        private void Awake()
        {
            network = NetworkManager.Singleton;
        }

        private void OnDestroy()
        {
            network.OnClientDisconnectCallback -= Dropped;
        }

        public bool Host(string save)
        {
            Account account = Config.Account;
            ServerConfig server = Config.Read().Server;

            UnityTransport transport = network.GetComponent<UnityTransport>();
            transport.UseEncryption = true;
            transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);
            transport.SetServerSecrets(account.Certificate, account.PrivateKeyPem);
            network.NetworkConfig.ConnectionData = Handshake.Encode(Config.Read().Client.Name, account, account.Certificate);

            if (!network.StartHost())
            {
                Log.Error("The host refused to start");
                return false;
            }

            Log.Info($"World listens on port {server.Port}, channel encrypted");

            // Spawned before the map loads, so no scene has to carry it: Netcode takes a spawned object along through the
            // scene change. On the host the map wakes up into a world that already has its state. A client loads the map
            // first and gets the state after it, together with the other network objects
            GameObject instance = Instantiate(state);
            instance.name = state.name;
            instance.GetComponent<NetworkObject>().Spawn();
            Log.Info("Game state is up");

            launch.Save = save;
            SceneEventProgressStatus status = network.SceneManager.LoadScene(world, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started) return true;

            Log.Error($"The world refused to load: {status}, shutting the host down");
            network.Shutdown();
            return false;
        }

        // A code that does not decode is turned down here and now; what happens after the client starts comes as Refused
        public Refusal Join()
        {
            ClientConfig client = Config.Read().Client;
            if (!Invite.TryDecode(client.Invite, out string address, out ushort port, out string certificate))
            {
                Log.Warn("Invite code is missing or malformed, staying in the menu");
                return new Refusal(BadInvite, BadInviteDetails);
            }

            UnityTransport transport = network.GetComponent<UnityTransport>();
            transport.UseEncryption = true;
            transport.SetConnectionData(address, port);
            transport.SetClientSecrets(Account.CommonName, certificate);
            network.NetworkConfig.ConnectionData = Handshake.Encode(client.Name, Config.Account, certificate);

            if (!network.StartClient())
            {
                Log.Warn($"The client refused to start for {address}:{port}");
                return new Refusal(BadInvite, BadInviteDetails);
            }

            network.OnClientDisconnectCallback += Dropped;
            Log.Info($"Heading for {address}:{port} as {client.Name}, channel encrypted");
            return null;
        }

        public void Cancel()
        {
            network.OnClientDisconnectCallback -= Dropped;
            network.Shutdown();
        }

        // The client dropped before it got into the world, the menu is still up to say why. Read here and not on the
        // stop: the shutdown that follows overwrites the transport's verdict
        private void Dropped(ulong ignored)
        {
            network.OnClientDisconnectCallback -= Dropped;
            NetworkTransport.DisconnectEvents verdict = network.NetworkConfig.NetworkTransport.DisconnectEvent;
            Log.Warn($"The client did not get into the world: {verdict}, {network.DisconnectReason}");

            switch (verdict)
            {
                case NetworkTransport.DisconnectEvents.ClosedByRemote:
                    Refused?.Invoke(new Refusal("Сервер отклонил подключение", network.DisconnectReason));
                    break;
                case NetworkTransport.DisconnectEvents.AuthenticationFailure:
                    Refused?.Invoke(new Refusal("Код подключения не подходит", "По этому адресу другой сервер. Попросите у хоста код ещё раз"));
                    break;
                default:
                    Refused?.Invoke(new Refusal("Сервер не отвечает", "Проверьте код подключения и что сервер запущен"));
                    break;
            }
        }
    }
}
