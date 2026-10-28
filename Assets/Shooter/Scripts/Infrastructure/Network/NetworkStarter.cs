using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkStarter : ShooterComponent
    {
        private const string AnyAddress = "0.0.0.0";
        private const string BadInviteTitle = "Неверный код подключения";
        private const string BadInviteDetails = "Попросите у хоста код ещё раз и вставьте его целиком";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private string world = "Map";

        private NetworkManager network;

        public event Action<NetworkStartFailure> JoinFailed;

        public string SaveLocationToLoad { get; private set; }

        public NetworkStartFailure StartFailureToShow { get; set; }

        protected override void ShooterAwake()
        {
            network = GetComponent<NetworkManager>();
        }

        protected override void ShooterOnDestroy()
        {
            StopWatchingJoin();
        }

        public NetworkStartFailure HostWorld(string saveLocation)
        {
            Account account = Config.Account;
            ServerConfig serverConfig = Config.GetGameConfig().Server;

            UnityTransport transport = network.GetComponent<UnityTransport>();
            transport.UseEncryption = true;
            transport.SetConnectionData(AnyAddress, serverConfig.Port, AnyAddress);
            transport.SetServerSecrets(account.Certificate, account.PrivateKeyPem);
            network.NetworkConfig.ConnectionData = NetworkHandshake.EncodeConnectionData(Config.GetGameConfig().Client.Name, account, account.Certificate);

            if (!network.StartHost())
            {
                Log.Warn($"The host refused to start on port {serverConfig.Port}");
                return new NetworkStartFailure("Не удалось запустить сервер", $"Возможно, порт {serverConfig.Port} занят. Смените его в настройках сервера");
            }

            Log.Info($"World listens on port {serverConfig.Port}, channel encrypted");

            SaveLocationToLoad = saveLocation;
            SceneEventProgressStatus status = network.SceneManager.LoadScene(world, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started) return null;

            Log.Error($"The world refused to load: {status}, shutting the host down");
            network.Shutdown();
            return new NetworkStartFailure("Не удалось запустить игру", "Карта не загрузилась");
        }

        public NetworkStartFailure JoinWorldByInvite()
        {
            ClientConfig clientConfig = Config.GetGameConfig().Client;
            if (!NetworkInvite.TryDecodeInviteCode(clientConfig.Invite, out string address, out ushort port, out string certificate))
            {
                Log.Warn("Invite code is missing or malformed, staying in the menu");
                return new NetworkStartFailure(BadInviteTitle, BadInviteDetails);
            }

            UnityTransport transport = network.GetComponent<UnityTransport>();
            transport.UseEncryption = true;
            transport.SetConnectionData(address, port);
            transport.SetClientSecrets(Account.CommonName, certificate);
            network.NetworkConfig.ConnectionData = NetworkHandshake.EncodeConnectionData(clientConfig.Name, Config.Account, certificate);

            if (!network.StartClient())
            {
                Log.Warn($"The client refused to start for {address}:{port}");
                return new NetworkStartFailure(BadInviteTitle, BadInviteDetails);
            }

            network.OnClientConnectedCallback += HandleClientConnected;
            network.OnClientDisconnectCallback += HandleClientDisconnected;
            Log.Info($"Heading for {address}:{port} as {clientConfig.Name}, channel encrypted");
            return null;
        }

        public void CancelJoin()
        {
            StopWatchingJoin();
            network.Shutdown();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (clientId != network.LocalClientId) return;

            StopWatchingJoin();
        }

        // The transport's DisconnectEvent is read here and not on the stop: the shutdown that follows overwrites it
        private void HandleClientDisconnected(ulong clientId)
        {
            StopWatchingJoin();
            NetworkTransport.DisconnectEvents disconnectEvent = network.NetworkConfig.NetworkTransport.DisconnectEvent;
            Log.Warn($"The client did not get into the world: {disconnectEvent}, {network.DisconnectReason}");

            switch (disconnectEvent)
            {
                case NetworkTransport.DisconnectEvents.ClosedByRemote:
                    JoinFailed?.Invoke(new NetworkStartFailure("Сервер отклонил подключение", network.DisconnectReason));
                    break;
                case NetworkTransport.DisconnectEvents.AuthenticationFailure:
                    JoinFailed?.Invoke(new NetworkStartFailure("Код подключения не подходит", "По этому адресу другой сервер. Попросите у хоста код ещё раз"));
                    break;
                default:
                    JoinFailed?.Invoke(new NetworkStartFailure("Сервер не отвечает", "Проверьте код подключения и что сервер запущен"));
                    break;
            }
        }

        private void StopWatchingJoin()
        {
            network.OnClientConnectedCallback -= HandleClientConnected;
            network.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }
}
