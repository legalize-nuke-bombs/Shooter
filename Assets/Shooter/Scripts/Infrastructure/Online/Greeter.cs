using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Infrastructure
{
    // Lets in only a client that proves it holds its account key, and keeps who came under which name and key
    [RequireComponent(typeof(NetworkManager))]
    public class Greeter : MonoBehaviour
    {
        private const int NameLimit = 24;
        private const string Nameless = "Player";
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<ulong, string> names = new();
        private readonly Dictionary<ulong, string> keys = new();

        private NetworkManager network;

        private void Awake()
        {
            network = GetComponent<NetworkManager>();
            network.NetworkConfig.ConnectionApproval = true;
            network.ConnectionApprovalCallback += Approve;
            network.OnClientDisconnectCallback += Forget;
            network.OnServerStopped += ForgetAll;
        }

        private void OnDestroy()
        {
            network.ConnectionApprovalCallback -= Approve;
            network.OnClientDisconnectCallback -= Forget;
            network.OnServerStopped -= ForgetAll;
        }

        public string NameOf(ulong client)
        {
            return names.TryGetValue(client, out string known) ? known : Nameless;
        }

        public string KeyOf(ulong client)
        {
            return keys.TryGetValue(client, out string carried) ? carried : null;
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (!Handshake.TryDecode(request.Payload, Config.Account.Certificate, out string given, out string publicKey))
            {
                response.Approved = false;
                response.Reason = "Не удалось подтвердить аккаунт игрока";
                Log.Warn($"Client {request.ClientNetworkId} rejected, could not prove key ownership");
                return;
            }

            given = given.Trim();
            names[request.ClientNetworkId] = given.Length == 0 ? Nameless : given.Substring(0, Mathf.Min(given.Length, NameLimit));
            keys[request.ClientNetworkId] = publicKey;

            response.Approved = true;
            response.CreatePlayerObject = false;

            Log.Info($"Client {request.ClientNetworkId} proved key ownership, approved as {names[request.ClientNetworkId]}");
        }

        private void Forget(ulong client)
        {
            names.Remove(client);
            keys.Remove(client);
        }

        // The network outlives a world: what one world knew of its clients must not greet the next one
        private void ForgetAll(bool ignored)
        {
            names.Clear();
            keys.Clear();
        }
    }
}
