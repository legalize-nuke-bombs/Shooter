using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    // Gives every player in the world a body: the one their key left here before, or a fresh one at a spawn point.
    // Works on the server, once the world says its save is in
    public class PlayerSpawner : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private readonly Dictionary<ulong, NetworkObject> bodies = new();

        private NetworkManager network;
        private Greeter greeter;
        private bool ready;

        private void Awake()
        {
            network = NetworkManager.Singleton;
            greeter = network.GetComponent<Greeter>();
            network.OnClientConnectedCallback += Welcome;
            network.OnClientDisconnectCallback += Forget;
        }

        private void OnDestroy()
        {
            network.OnClientConnectedCallback -= Welcome;
            network.OnClientDisconnectCallback -= Forget;
        }

        // Players who came while the world was loading are already connected: they get their bodies here
        public void Ready()
        {
            ready = true;
            foreach (ulong client in network.ConnectedClientsIds)
            {
                Embody(client);
            }

            foreach (Player player in Registers.Of<Player>(Inactive.Include))
            {
                NetworkObject body = player.GetComponent<NetworkObject>();
                if (!bodies.ContainsValue(body)) body.gameObject.SetActive(false);
            }
        }

        private void Welcome(ulong client)
        {
            if (!network.IsServer || !ready) return;

            Embody(client);
        }

        private void Embody(ulong client)
        {
            string name = greeter.NameOf(client);
            string key = greeter.KeyOf(client);

            Player returning = key == null ? null : Player.OfKey(key, Inactive.Include);
            NetworkObject body;
            if (returning != null)
            {
                body = returning.GetComponent<NetworkObject>();
                body.gameObject.SetActive(true);
            }
            else
            {
                MainSpawnPoint point = MainSpawnPoint.Pick();
                Transform at = point == null ? transform : point.transform;
                GameObject fresh = Spawner.Spawn(network.NetworkConfig.PlayerPrefab, at.position, at.rotation);
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
            if (!bodies.Remove(client, out NetworkObject body) || body == null) return;

            body.gameObject.SetActive(false);
            Log.Info($"Client {client} left the world, body switched off");
        }
    }
}
