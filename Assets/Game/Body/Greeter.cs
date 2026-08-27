using System.Collections.Generic;
using System.Text;
using Shooter.Accounts;
using Shooter.Configuring;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(NetworkManager))]
    public class Greeter : MonoBehaviour
    {
        private const int NameLimit = 24;
        private const string Nameless = "Player";
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<ulong, string> names = new();

        private NetworkManager network;

        private void Awake()
        {
            network = GetComponent<NetworkManager>();
            network.NetworkConfig.ConnectionApproval = true;
            network.ConnectionApprovalCallback += Approve;
            network.OnClientConnectedCallback += Welcome;
            network.OnClientDisconnectCallback += Forget;
        }

        private void OnDestroy()
        {
            if (network == null) return;

            network.ConnectionApprovalCallback -= Approve;
            network.OnClientConnectedCallback -= Welcome;
            network.OnClientDisconnectCallback -= Forget;
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (!Handshake.TryDecode(request.Payload, Config.Account.Certificate, out string given, out string publicKey))
            {
                response.Approved = false;
                Log.Warn($"Client {request.ClientNetworkId} rejected, could not prove key ownership");
                return;
            }

            given = given.Trim();
            string name = given.Length == 0 ? Nameless : given.Substring(0, Mathf.Min(given.Length, NameLimit));

            names[request.ClientNetworkId] = name;
            response.Approved = true;
            response.CreatePlayerObject = true;

            Log.Info($"Client {request.ClientNetworkId} proved key ownership {Account.Fingerprint(publicKey)}, approved as {name}");
        }

        private void Welcome(ulong client)
        {
            if (!network.IsServer) return;

            if (!network.ConnectedClients.TryGetValue(client, out NetworkClient connected) ||
                connected.PlayerObject == null)
            {
                Log.Warn($"Client {client} connected without a player object");
                return;
            }

            string name = names.TryGetValue(client, out string known) ? known : Nameless;
            connected.PlayerObject.GetComponent<AbsoluteNameable>()?.Rename(name);
            connected.PlayerObject.name = name;

            Transform at = MainSpawnPoint.Current == null
                ? connected.PlayerObject.transform
                : MainSpawnPoint.Current.transform;
            connected.PlayerObject.GetComponent<Movement>()?.Teleport(at.position, at.eulerAngles.y);

            Log.Info(
                $"Client {client} entered the world as {name} at {at.position}, players online {network.ConnectedClients.Count}");
        }

        private void Forget(ulong client)
        {
            if (!names.Remove(client)) return;

            Log.Info($"Client {client} left the world");
        }
    }
}
