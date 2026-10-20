using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class PlayerSpawner : ShooterBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SaveLoaderOnStartup saveLoaderOnStartup;

        private readonly Dictionary<ulong, NetworkObject> bodiesByClientId = new();

        private NetworkManager network;
        private NetworkConnectionApprover networkConnectionApprover;
        private bool worldLoaded;

        protected override void ShooterAwake()
        {
            network = NetworkManager.Singleton;
            networkConnectionApprover = network.GetComponent<NetworkConnectionApprover>();
            network.OnClientConnectedCallback += HandleClientConnected;
            network.OnClientDisconnectCallback += HandleClientDisconnected;
            saveLoaderOnStartup.Loaded += HandleSaveLoaded;
        }

        protected override void ShooterOnDestroy()
        {
            network.OnClientConnectedCallback -= HandleClientConnected;
            network.OnClientDisconnectCallback -= HandleClientDisconnected;
            saveLoaderOnStartup.Loaded -= HandleSaveLoaded;
        }

        // Players who came while the world was loading are already connected: they get their bodies here
        private void HandleSaveLoaded()
        {
            worldLoaded = true;
            foreach (ulong clientId in network.ConnectedClientsIds)
            {
                GiveBodyToClient(clientId);
            }

            foreach (Player player in Registers.GetAll<Player>(RegisterInactivePolicy.Include))
            {
                NetworkObject body = player.GetComponent<NetworkObject>();
                if (!bodiesByClientId.ContainsValue(body)) body.gameObject.SetActive(false);
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!network.IsServer || !worldLoaded) return;

            GiveBodyToClient(clientId);
        }

        private void GiveBodyToClient(ulong clientId)
        {
            string playerName = networkConnectionApprover.GetPlayerName(clientId);
            string publicKey = networkConnectionApprover.GetPublicKey(clientId);

            Player returningPlayer = Player.FindByPublicKey(publicKey, RegisterInactivePolicy.Include);
            NetworkObject body;
            if (returningPlayer != null)
            {
                body = returningPlayer.GetComponent<NetworkObject>();
                body.gameObject.SetActive(true);
            }
            else
            {
                MainSpawnPoint mainSpawnPoint = MainSpawnPoint.PickRandom();
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
            bodiesByClientId[clientId] = body;
            body.GetComponent<AbsoluteNameable>()?.RenameTo(playerName);
            body.name = playerName;

            Log.Info($"Client {clientId} entered the world as {playerName}, players online {network.ConnectedClients.Count}");
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!bodiesByClientId.Remove(clientId, out NetworkObject body) || body == null) return;

            body.gameObject.SetActive(false);
            Log.Info($"Client {clientId} left the world, body switched off");
        }
    }
}
