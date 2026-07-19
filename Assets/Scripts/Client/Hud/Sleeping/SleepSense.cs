using UnityEngine;
using Shooter.Client.Aiming;
using Shooter.Client.Worlds;
using Shooter.Server.Worlds.Entities.Chronology;
using Shooter.Server.Worlds.Entities.Players;
using Shooter.Server.Worlds.Entities.Sleeping;

namespace Shooter.Client.Hud.Sleeping
{
    [RequireComponent(typeof(Aim))]
    public class SleepSense : MonoBehaviour
    {
        private Aim aim;

        public bool MySleeping => Local(out PlayerState me) && me.Sleeping;

        public bool WorldAsleep
        {
            get
            {
                ClientWorld world = NetworkClient.Instance?.World;
                return world?.Sleep != null && world.Sleep.WorldAsleep;
            }
        }

        public bool CanSleep => !MySleeping && Night
                                && aim.Target != null
                                && aim.Target.Value.distance <= Sleep.UseReach
                                && Sleep.IsBed(aim.Target.Value.collider.name);

        private bool Night
        {
            get
            {
                ClientWorld world = NetworkClient.Instance?.World;
                return world?.Clock != null && DayCycle.IsNight(DayCycle.FractionOf(world.Clock.Timestamp));
            }
        }

        private void Awake()
        {
            aim = GetComponent<Aim>();
        }

        private bool Local(out PlayerState me)
        {
            me = null;
            ClientWorld world = NetworkClient.Instance?.World;
            return world?.Players != null && world.Players.TryGetValue(world.PlayerId, out me);
        }
    }
}
