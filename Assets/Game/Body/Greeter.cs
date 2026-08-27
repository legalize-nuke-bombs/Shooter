using System.Collections.Generic;
using Shooter.Configuring;
using Shooter.Game.Core;
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
        private readonly Dictionary<ulong, string> keys = new();
        private readonly Dictionary<ulong, NetworkObject> bodies = new();

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
            keys[request.ClientNetworkId] = publicKey;

            response.Approved = true;
            response.CreatePlayerObject = Player.OfKey(publicKey, Inactive.Include) == null;

            Log.Info(
                $"Client {request.ClientNetworkId} proved key ownership, approved as {name}, {(response.CreatePlayerObject ? "fresh body" : "reclaiming a body")}");
        }

        private void Welcome(ulong client)
        {
            if (!network.IsServer) return;
            if (!network.ConnectedClients.TryGetValue(client, out NetworkClient connected)) return;

            string name = names.TryGetValue(client, out string known) ? known : Nameless;
            string key = keys.TryGetValue(client, out string carried) ? carried : null;

            NetworkObject body = connected.PlayerObject;
            if (body != null)
            {
                Player fresh = body.GetComponent<Player>();
                if (fresh != null) fresh.PublicKey = key;

                Transform at = MainSpawnPoint.Current == null ? body.transform : MainSpawnPoint.Current.transform;
                body.GetComponent<Movement>()?.Teleport(at.position, at.eulerAngles.y);
            }
            else
            {
                Player returning = key == null ? null : Player.OfKey(key, Inactive.Include);
                if (returning == null)
                {
                    Log.Warn($"Client {client} returned but no body carried the key, entering without one");
                    return;
                }

                body = returning.GetComponent<NetworkObject>();
                body.gameObject.SetActive(true);
                body.ChangeOwnership(client);
            }

            body.DontDestroyWithOwner = true;
            bodies[client] = body;
            body.GetComponent<AbsoluteNameable>()?.Rename(name);
            body.name = name;

            Log.Info($"Client {client} entered the world as {name}, players online {network.ConnectedClients.Count}");
        }

        private void Forget(ulong client)
        {
            names.Remove(client);
            keys.Remove(client);
            if (!bodies.Remove(client, out NetworkObject body) || body == null) return;

            body.gameObject.SetActive(false);
            Log.Info($"Client {client} left the world, body switched off");
        }
    }
}
