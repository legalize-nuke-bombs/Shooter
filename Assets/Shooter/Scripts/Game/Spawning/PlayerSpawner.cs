using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    // Gives every player in the world a body: the one their key left here before, or a fresh one at a spawn point.
    // Works on the server, once the save is in
    public class PlayerSpawner : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private SaveLoaderOnStartup saveLoaderOnStartup;

        private readonly Dictionary<ulong, NetworkObject> bodies = new();

        private NetworkManager network;
        private NetworkConnectionApprover networkConnectionApprover;
        private bool ready;

        protected override void ShooterAwake()
        {
            network = NetworkManager.Singleton;
            networkConnectionApprover = network.GetComponent<NetworkConnectionApprover>();
            network.OnClientConnectedCallback += Welcome;
            network.OnClientDisconnectCallback += Forget;
            saveLoaderOnStartup.Loaded += HandleSaveLoaded;
        }

        protected override void ShooterOnDestroy()
        {
            network.OnClientConnectedCallback -= Welcome;
            network.OnClientDisconnectCallback -= Forget;
            saveLoaderOnStartup.Loaded -= HandleSaveLoaded;
        }

        // Players who came while the world was loading are already connected: they get their bodies here
        private void HandleSaveLoaded()
        {
            ready = true;
            foreach (ulong clientId in network.ConnectedClientsIds)
            {
                Embody(clientId);
            }

            foreach (Player player in Registers.GetAll<Player>(RegisterInactivePolicy.Include))
            {
                NetworkObject body = player.GetComponent<NetworkObject>();
                if (!bodies.ContainsValue(body)) body.gameObject.SetActive(false);
            }
        }

        private void Welcome(ulong clientId)
        {
            if (!network.IsServer || !ready) return;

            Embody(clientId);
        }

        private void Embody(ulong clientId)
        {
            string playerName = networkConnectionApprover.GetPlayerName(clientId);
            string publicKey = networkConnectionApprover.GetPublicKey(clientId);

            Player returning = Player.OfKey(publicKey, RegisterInactivePolicy.Include);
            NetworkObject body;
            if (returning != null)
            {
                body = returning.GetComponent<NetworkObject>();
                body.gameObject.SetActive(true);
            }
            else
            {
                MainSpawnPoint mainSpawnPoint = MainSpawnPoint.Pick();
                Transform spawnTransform = mainSpawnPoint == null ? transform : mainSpawnPoint.transform;
                GameObject freshBody = NetworkSpawner.InstantiateAndSpawn(network.NetworkConfig.PlayerPrefab, spawnTransform.position, spawnTransform.rotation);
                if (freshBody == null)
                {
                    Log.Error($"Client {clientId} could not be given a body");
                    return;
                }

                body = freshBody.GetComponent<NetworkObject>();
                Player player = freshBody.GetComponent<Player>();
                if (player != null) player.PublicKey = publicKey;
            }

            body.ChangeOwnership(clientId);
            body.DontDestroyWithOwner = true;
            bodies[clientId] = body;
            body.GetComponent<AbsoluteNameable>()?.Rename(playerName);
            body.name = playerName;

            Log.Info($"Client {clientId} entered the world as {playerName}, players online {network.ConnectedClients.Count}");
        }

        private void Forget(ulong clientId)
        {
            if (!bodies.Remove(clientId, out NetworkObject body) || body == null) return;

            body.gameObject.SetActive(false);
            Log.Info($"Client {clientId} left the world, body switched off");
        }
    }
}
