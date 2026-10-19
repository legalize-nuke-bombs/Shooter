using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkConnectionApprover : ShooterBehaviour
    {
        private const int PlayerNameLengthLimit = 24;
        private const string DefaultPlayerName = "Player";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        private readonly Dictionary<ulong, string> playerNamesByClientId = new();
        private readonly Dictionary<ulong, string> publicKeysByClientId = new();

        private NetworkManager network;

        protected override void ShooterAwake()
        {
            network = GetComponent<NetworkManager>();
            network.NetworkConfig.ConnectionApproval = true;
            network.ConnectionApprovalCallback += ApproveOrRejectConnection;
            network.OnClientDisconnectCallback += ForgetDisconnectedClient;
            network.OnServerStopped += ForgetAllClients;
        }

        protected override void ShooterOnDestroy()
        {
            network.ConnectionApprovalCallback -= ApproveOrRejectConnection;
            network.OnClientDisconnectCallback -= ForgetDisconnectedClient;
            network.OnServerStopped -= ForgetAllClients;
        }

        public string GetPlayerName(ulong clientId)
        {
            return playerNamesByClientId[clientId];
        }

        public string GetPublicKey(ulong clientId)
        {
            return publicKeysByClientId[clientId];
        }

        private void ApproveOrRejectConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            ulong clientId = request.ClientNetworkId;
            if (!NetworkHandshake.TryDecodeConnectionData(request.Payload, Config.Account.Certificate, out string playerName, out string publicKey))
            {
                response.Approved = false;
                response.Reason = "Не удалось подтвердить аккаунт игрока";
                Log.Warn($"Client {clientId} rejected, could not prove key ownership");
                return;
            }

            if (TryFindClientWithPublicKey(publicKey, out ulong earlierClientId))
            {
                if (earlierClientId == NetworkManager.ServerClientId)
                {
                    response.Approved = false;
                    response.Reason = "На этом аккаунте уже играет хост";
                    Log.Warn($"Client {clientId} rejected, it carries the host's own key");
                    return;
                }

                Log.Info($"Client {clientId} carries the key of client {earlierClientId}, the earlier connection is dropped");
                network.DisconnectClient(earlierClientId);
            }

            playerName = playerName.Trim();
            playerNamesByClientId[clientId] = playerName.Length == 0 ? DefaultPlayerName : playerName.Substring(0, Mathf.Min(playerName.Length, PlayerNameLengthLimit));
            publicKeysByClientId[clientId] = publicKey;

            response.Approved = true;
            response.CreatePlayerObject = false;

            Log.Info($"Client {clientId} proved key ownership, approved as {playerNamesByClientId[clientId]}");
        }

        private bool TryFindClientWithPublicKey(string publicKey, out ulong clientId)
        {
            foreach (KeyValuePair<ulong, string> entry in publicKeysByClientId)
            {
                if (entry.Value == publicKey)
                {
                    clientId = entry.Key;
                    return true;
                }
            }

            clientId = default;
            return false;
        }

        private void ForgetDisconnectedClient(ulong clientId)
        {
            playerNamesByClientId.Remove(clientId);
            publicKeysByClientId.Remove(clientId);
        }

        private void ForgetAllClients(bool wasHost)
        {
            playerNamesByClientId.Clear();
            publicKeysByClientId.Clear();
        }
    }
}
