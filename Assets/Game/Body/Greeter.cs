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
        private readonly List<ulong> pending = new();

        private NetworkManager network;
        private bool ready;

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

        public void Ready()
        {
            if (!network.IsServer) return;

            ready = true;
            foreach (ulong client in pending) Embody(client);
            pending.Clear();

            foreach (Player player in Registers.Current.Of<Player>(Inactive.Include))
            {
                NetworkObject body = player.GetComponent<NetworkObject>();
                if (!bodies.ContainsValue(body)) body.gameObject.SetActive(false);
            }
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
            names[request.ClientNetworkId] = given.Length == 0 ? Nameless : given.Substring(0, Mathf.Min(given.Length, NameLimit));
            keys[request.ClientNetworkId] = publicKey;

            response.Approved = true;
            response.CreatePlayerObject = false;

            Log.Info($"Client {request.ClientNetworkId} proved key ownership, approved as {names[request.ClientNetworkId]}");
        }

        private void Welcome(ulong client)
        {
            if (!network.IsServer) return;

            if (ready) Embody(client);
            else pending.Add(client);
        }

        private void Embody(ulong client)
        {
            string name = names.TryGetValue(client, out string known) ? known : Nameless;
            string key = keys.TryGetValue(client, out string carried) ? carried : null;

            Player returning = key == null ? null : Player.OfKey(key, Inactive.Include);
            NetworkObject body;
            if (returning != null)
            {
                body = returning.GetComponent<NetworkObject>();
                body.gameObject.SetActive(true);
            }
            else
            {
                Transform at = MainSpawnPoint.Current == null ? transform : MainSpawnPoint.Current.transform;
                GameObject fresh = Spawner.Current.Spawn(network.NetworkConfig.PlayerPrefab, at.position, at.rotation);
                if (fresh == null)
                {
                    Log.Error($"Client {client} could not be given a body");
                    return;
                }

                body = fresh.GetComponent<NetworkObject>();
                Player player = fresh.GetComponent<Player>();
                if (player != null) player.PublicKey = key;
            }

            body.ChangeOwnership(client);
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
            pending.Remove(client);
            if (!bodies.Remove(client, out NetworkObject body) || body == null) return;

            body.gameObject.SetActive(false);
            Log.Info($"Client {client} left the world, body switched off");
        }
    }
}
