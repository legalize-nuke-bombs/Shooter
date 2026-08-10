using System;
using System.Collections.Generic;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.World
{
    public class ClockTrigger : Trigger
    {
        private static readonly Journal Log = Logs.Here();

        [Serializable]
        public struct Cooldown
        {
            [SerializeField] public int days;
            [SerializeField] public int hours;
            [SerializeField] public int minutes;

            public double Timestamp => days * 3600 * 24 + hours * 3600 + minutes * 60;
        }

        [SerializeField] private Cooldown veryFirstCooldown = new Cooldown { days = 1 };
        [SerializeField] private Cooldown interCooldown = new Cooldown { days = 1 };
        [SerializeField] private bool useInvokeMaxNum = false;
        [SerializeField] private int invokeMaxNum = 0;

        private int invokes = 0;

        protected override void Awake()
        {
            base.Awake();

            NetworkManager network = NetworkManager.Singleton;
            enabled = network != null && network.IsServer;
            if (network == null) return;

            network.OnServerStarted += WakeUp;
            network.OnServerStopped += Sleep;
        }

        private void OnDestroy()
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null) return;

            network.OnServerStarted -= WakeUp;
            network.OnServerStopped -= Sleep;
        }

        private void WakeUp()
        {
            enabled = true;
        }

        private void Sleep(bool wasHost)
        {
            enabled = false;
        }

        private double? NextInvoke()
        {
            if (useInvokeMaxNum && invokes >= invokeMaxNum)
            {
                return null;
            }
            return veryFirstCooldown.Timestamp + invokes * interCooldown.Timestamp;
        }

        private void Update()
        {
            double? nextInvoke = NextInvoke();
            if (nextInvoke == null)
            {
                enabled = false;
                return;
            }

            if (Environment.Current.Clock.Timestamp >= nextInvoke)
            {
                OnTrigger();
            }
        }

        private void OnTrigger()
        {
            invokes++;
            Log.Info($"Entity {name} is invoking for {invokes} time");

            PersistentIds ids = Environment.Current.PersistentIds;
            List<PersistentId> characters = ids.GetFiltered("Character");

            foreach (PersistentId character in characters)
            {
                base.OnTrigger(character);
            }
        }
    }
}
