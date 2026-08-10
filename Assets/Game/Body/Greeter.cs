using System.Collections.Generic;
using System.Text;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(NetworkManager))]
    public class Greeter : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

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

            Log.Info($"Client {request.ClientNetworkId} approved as {name}");
        }

        private void Welcome(ulong client)
        {
            if (!network.IsServer) return;

            if (!network.ConnectedClients.TryGetValue(client, out NetworkClient connected) || connected.PlayerObject == null)
            {
                Log.Warn($"Client {client} connected without a player object");
                return;
            }

            string name = names.TryGetValue(client, out string known) ? known : Nameless;
            connected.PlayerObject.GetComponent<AbsoluteNameable>()?.Rename(name);
            connected.PlayerObject.name = name;

            Transform at = Environment.Current == null
                ? connected.PlayerObject.transform
                : Environment.Current.Spawn;
            connected.PlayerObject.GetComponent<Movement>()?.Teleport(at.position, at.eulerAngles.y);

            Log.Info($"Client {client} entered the world as {name} at {at.position}, players online {network.ConnectedClients.Count}");
        }

        private void Forget(ulong client)
        {
            if (!names.Remove(client)) return;

            Log.Info($"Client {client} left the world");
        }
    }
}
