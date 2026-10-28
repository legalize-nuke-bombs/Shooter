using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Skin))]
    public class SkinHiderWhileDead : NetworkBehaviour, IShooterLateUpdateable
    {
        private Health health;
        private Skin skin;
        private bool modelVisible;

        private void Awake()
        {
            health = GetComponent<Health>();
            skin = GetComponent<Skin>();
            enabled = false;
        }

        // Applied after the rest of the frame: on death the corpse takes the held weapon in the same frame, and the
        // weapon rig has to come down while the model is still active, or the animator warns about it on respawn
        public void ShooterLateUpdate()
        {
            skin.SetModelVisible(modelVisible);
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            ShowModelWhileAlive(health.IsAlive);
            health.AliveChanged += ShowModelWhileAlive;
        }

        public override void OnNetworkDespawn()
        {
            health.AliveChanged -= ShowModelWhileAlive;
        }

        private void ShowModelWhileAlive(bool alive)
        {
            modelVisible = alive;
            enabled = true;
        }
    }
}
