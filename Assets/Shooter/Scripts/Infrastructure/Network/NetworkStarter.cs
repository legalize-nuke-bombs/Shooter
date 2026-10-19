using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkStarter : ShooterBehaviour
    {
        private const string AnyAddress = "0.0.0.0";
        private const string BadInvite = "Неверный код подключения";
        private const string BadInviteDetails = "Попросите у хоста код ещё раз и вставьте его целиком";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private string world = "Map";

        private NetworkManager network;

        public event Action<Refusal> Refused;

        public string Save { get; private set; }

        public Refusal Ending { get; set; }

        protected override void ShooterAwake()
        {
            network = GetComponent<NetworkManager>();
        }

        protected override void ShooterOnDestroy()
        {
            StopWatchingJoin();
        }

        public Refusal Host(string save)
        {
            Account account = Config.Account;
            ServerConfig server = Config.Read().Server;

            UnityTransport transport = network.GetComponent<UnityTransport>();
            transport.UseEncryption = true;
            transport.SetConnectionData(AnyAddress, server.Port, AnyAddress);
            transport.SetServerSecrets(account.Certificate, account.PrivateKeyPem);
            network.NetworkConfig.ConnectionData = NetworkHandshake.Encode(Config.Read().Client.Name, account, account.Certificate);

            if (!network.StartHost())
            {
                Log.Warn($"The host refused to start on port {server.Port}");
                return new Refusal("Не удалось запустить сервер", $"Возможно, порт {server.Port} занят. Смените его в настройках сервера");
            }

            Log.Info($"World listens on port {server.Port}, channel encrypted");

            Save = save;
            SceneEventProgressStatus status = network.SceneManager.LoadScene(world, LoadSceneMode.Single);
            if (status == SceneEventProgressStatus.Started) return null;

            Log.Error($"The world refused to load: {status}, shutting the host down");
            network.Shutdown();
            return new Refusal("Не удалось запустить игру", "Карта не загрузилась");
        }

        public Refusal Join()
        {
            ClientConfig client = Config.Read().Client;
            if (!NetworkInvite.TryDecode(client.Invite, out string address, out ushort port, out string certificate))
            {
                Log.Warn("Invite code is missing or malformed, staying in the menu");
                return new Refusal(BadInvite, BadInviteDetails);
            }

            UnityTransport transport = network.GetComponent<UnityTransport>();
            transport.UseEncryption = true;
            transport.SetConnectionData(address, port);
            transport.SetClientSecrets(Account.CommonName, certificate);
            network.NetworkConfig.ConnectionData = NetworkHandshake.Encode(client.Name, Config.Account, certificate);

            if (!network.StartClient())
            {
                Log.Warn($"The client refused to start for {address}:{port}");
                return new Refusal(BadInvite, BadInviteDetails);
            }

            network.OnClientConnectedCallback += Entered;
            network.OnClientDisconnectCallback += Dropped;
            Log.Info($"Heading for {address}:{port} as {client.Name}, channel encrypted");
            return null;
        }

        public void Cancel()
        {
            StopWatchingJoin();
            network.Shutdown();
        }

        private void Entered(ulong client)
        {
            if (client != network.LocalClientId) return;

            StopWatchingJoin();
        }

        // Read here and not on the stop: the shutdown that follows overwrites the transport's verdict
        private void Dropped(ulong ignored)
        {
            StopWatchingJoin();
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

        private void StopWatchingJoin()
        {
            network.OnClientConnectedCallback -= Entered;
            network.OnClientDisconnectCallback -= Dropped;
        }
    }
}
