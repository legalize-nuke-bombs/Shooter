using System;
using Unity.Netcode;
using Shooter.Logging;

namespace Shooter.Game.Body
{
    public class Hands : NetworkBehaviour, IMortal, IDigestible
    {
        private readonly NetworkVariable<HandsAction> action = new NetworkVariable<HandsAction>();

        private Action complete;
        private float remaining;
        private bool interruptible;

        public HandsAction Action => action.Value;

        public bool Free => action.Value == HandsAction.None;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick += Step;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            NetworkManager.NetworkTickSystem.Tick -= Step;
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
            if (!Free) Log.Info("Hands action {} of entity {} preempted by {}", Action, name, wanted);

            Take(wanted, duration, interruptible, complete);
            return true;
        }

        public void Interrupt()
        {
            if (Free) return;

            Log.Info("Hands action {} of entity {} interrupted", Action, name);
            action.Value = HandsAction.None;
            complete = null;
            remaining = 0f;
        }

        public void Died()
        {
            Interrupt();
        }

        public string Digest()
        {
            return Free ? null : "Занят: " + Action;
        }

        private void Step()
        {
            if (Free) return;

            remaining -= NetworkManager.LocalTime.FixedDeltaTime;
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
