using System.Collections.Generic;
using System.Text;
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

        private readonly Dictionary<ulong, string> names = new Dictionary<ulong, string>();

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

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            string given = Encoding.UTF8.GetString(request.Payload ?? new byte[0]).Trim();
            string name = given.Length == 0 ? Nameless : given.Substring(0, Mathf.Min(given.Length, NameLimit));

            names[request.ClientNetworkId] = name;
            response.Approved = true;
            response.CreatePlayerObject = true;

            Log.Info("Client {} approved as {}", request.ClientNetworkId, name);
        }

        private void Welcome(ulong client)
        {
            if (!network.IsServer) return;

            if (!network.ConnectedClients.TryGetValue(client, out NetworkClient connected) || connected.PlayerObject == null)
            {
                Log.Warn("Client {} connected without a player object", client);
                return;
            }

            string name = names.TryGetValue(client, out string known) ? known : Nameless;
            connected.PlayerObject.GetComponent<AbsoluteNameable>()?.Rename(name);
            connected.PlayerObject.name = name;

            Log.Info("Client {} entered the world as {}, players online {}", client, name, network.ConnectedClients.Count);
        }

        private void Forget(ulong client)
        {
            if (!names.Remove(client)) return;

            Log.Info("Client {} left the world", client);
        }
    }
}
