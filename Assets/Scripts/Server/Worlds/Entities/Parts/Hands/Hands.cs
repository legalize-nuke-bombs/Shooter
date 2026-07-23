using Shooter.Logging;

namespace Shooter.Server.Worlds.Entities.Parts.Hands
{
    public sealed class Hands : Part
    {
        public HandsAction Action { get; private set; } = HandsAction.None;

        public bool Free => Action == HandsAction.None;

        private System.Action complete;
        private float remaining;
        private bool interruptible;

        public bool TryTake(HandsAction action, float duration, bool interruptible, System.Action complete)
        {
            if (!Free) return false;
            Take(action, duration, interruptible, complete);
            return true;
        }

        public bool Preempt(HandsAction action, float duration, bool interruptible, System.Action complete)
        {
            if (!Free && !this.interruptible) return false;
            if (!Free) Log.Info("Hands action {} preempted by {}", Action, action);
            Take(action, duration, interruptible, complete);
            return true;
        }

        public void Interrupt()
        {
            if (Free) return;
            Log.Info("Hands action {} interrupted", Action);
            Action = HandsAction.None;
            complete = null;
            remaining = 0f;
        }

        public override void Tick(Entity self, float dt)
        {
            if (Free) return;
            remaining -= dt;
            if (remaining > 0f) return;

            System.Action finished = complete;
            Action = HandsAction.None;
            complete = null;
            finished?.Invoke();
        }

        public override PartState State()
        {
            return new HandsState { Action = Action };
        }

        private void Take(HandsAction action, float duration, bool interruptible, System.Action complete)
        {
            Action = action;
            remaining = duration;
            this.interruptible = interruptible;
            this.complete = complete;
        }
    }
}
