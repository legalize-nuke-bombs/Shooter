using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkConnectionApprover : ShooterBehaviour
    {
        private const int NameLimit = 24;
        private const string Nameless = "Player";
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<ulong, string> names = new();
        private readonly Dictionary<ulong, string> publicKeys = new();

        private NetworkManager network;

        protected override void ShooterAwake()
        {
            network = GetComponent<NetworkManager>();
            network.NetworkConfig.ConnectionApproval = true;
            network.ConnectionApprovalCallback += Approve;
            network.OnClientDisconnectCallback += Forget;
            network.OnServerStopped += ForgetAll;
        }

        protected override void ShooterOnDestroy()
        {
            network.ConnectionApprovalCallback -= Approve;
            network.OnClientDisconnectCallback -= Forget;
            network.OnServerStopped -= ForgetAll;
        }

        public string NameOf(ulong client)
        {
            return names[client];
        }

        public string PublicKeyOf(ulong client)
        {
            return publicKeys[client];
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            ulong client = request.ClientNetworkId;
            if (!NetworkHandshake.TryDecode(request.Payload, Config.Account.Certificate, out string name, out string publicKey))
            {
                response.Approved = false;
                response.Reason = "Не удалось подтвердить аккаунт игрока";
                Log.Warn($"Client {client} rejected, could not prove key ownership");
                return;
            }

            if (TryFindClientOf(publicKey, out ulong earlier))
            {
                if (earlier == NetworkManager.ServerClientId)
                {
                    response.Approved = false;
                    response.Reason = "На этом аккаунте уже играет хост";
                    Log.Warn($"Client {client} rejected, it carries the host's own key");
                    return;
                }

                Log.Info($"Client {client} carries the key of client {earlier}, the earlier connection is dropped");
                network.DisconnectClient(earlier);
            }

            name = name.Trim();
            names[client] = name.Length == 0 ? Nameless : name.Substring(0, Mathf.Min(name.Length, NameLimit));
            publicKeys[client] = publicKey;

            response.Approved = true;
            response.CreatePlayerObject = false;

            Log.Info($"Client {client} proved key ownership, approved as {names[client]}");
        }

        private bool TryFindClientOf(string publicKey, out ulong client)
        {
            foreach (KeyValuePair<ulong, string> entry in publicKeys)
            {
                if (entry.Value == publicKey)
                {
                    client = entry.Key;
                    return true;
                }
            }

            client = default;
            return false;
        }

        private void Forget(ulong client)
        {
            names.Remove(client);
            publicKeys.Remove(client);
        }

        private void ForgetAll(bool ignored)
        {
            names.Clear();
            publicKeys.Clear();
        }
    }
}
