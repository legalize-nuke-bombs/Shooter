using System;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Hands : NetworkBehaviour, IMortal, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<HandsAction> action = new();

        private Action complete;
        private bool interruptible;
        private float remaining;

        public HandsAction Action => action.Value;

        public bool Free => action.Value == HandsAction.None;

        public string Digest(DigestionDetail detail)
        {
            return Free ? null : "Busy: " + Action;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public void Died()
        {
            Interrupt();
        }

        private void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            enabled = false;
        }

        public bool TryTake(HandsAction wanted, float duration, bool interruptible, Action complete)
        {
            if (!Free) return false;

            Take(wanted, duration, interruptible, complete);
            return true;
        }

        public bool TryPreempt(HandsAction wanted, float duration, bool interruptible, Action complete)
        {
            if (!Free && !this.interruptible) return false;
            if (!Free) Log.Info($"Hands action {Action} of entity {name} preempted by {wanted}");

            Take(wanted, duration, interruptible, complete);
            return true;
        }

        public void Interrupt()
        {
            if (Free) return;

            Log.Info($"Hands action {Action} of entity {name} interrupted");
            action.Value = HandsAction.None;
            complete = null;
            remaining = 0f;
        }

        private void Update()
        {
            if (Free) return;

            remaining -= Time.deltaTime;
            if (remaining > 0f) return;

            Action finished = complete;
            action.Value = HandsAction.None;
            complete = null;
            finished?.Invoke();
        }

        private void Take(HandsAction wanted, float duration, bool interruptible, Action complete)
        {
            action.Value = wanted;
            remaining = duration;
            this.interruptible = interruptible;
            this.complete = complete;
        }
    }
}
