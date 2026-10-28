using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class Hands : ShooterNetworkComponent, IDigestible, IShooterUpdateable
    {
        private const string DeadRefusal = "dead";
        private const string BusyHandsRefusal = "hands busy";
        private static readonly Journal Log = Logs.Here();

        private readonly NetworkVariable<Work> work = new();
        private readonly List<Func<ActionType, float, bool>> checks = new();

        private Action actionCompletionCallback;
        private bool actionInterruptible;
        private float actionStartedAt;
        private float actionDuration;
        private Health health;

        public ActionType CurrentAction => work.Value.Action;

        public bool AreFree => CurrentAction == ActionType.None;

        public event Func<ActionType, float, bool> Checking
        {
            add => checks.Add(value);
            remove => checks.Remove(value);
        }

        public event Action<ActionType, float> Performed;

        public event Action<ActionType, float> ActionStarted;

        public string Digest(DigestionDetail detail)
        {
            return AreFree ? null : "Busy: " + CurrentAction;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
            enabled = false;
        }

        protected override void ShooterOnEnable()
        {
            health.Died += InterruptAction;
        }

        protected override void ShooterOnDisable()
        {
            health.Died -= InterruptAction;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                enabled = true;
            }
            else
            {
                work.OnValueChanged += HandleWorkReplicated;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                enabled = false;
            }
            else
            {
                work.OnValueChanged -= HandleWorkReplicated;
            }
        }

        public bool TryPerform(ActionType action, float duration)
        {
            return TryPerform(action, duration, out _);
        }

        public bool TryPerform(ActionType action, float duration, out string refusal)
        {
            refusal = FindRefusal(action, duration, false);
            if (refusal != null)
            {
                Log.Debug($"Entity {name} can not perform {action}, reason: {refusal}");
                return false;
            }

            Performed?.Invoke(action, duration);
            return true;
        }

        public bool TryStartAction(ActionType action, float duration, bool interruptible, Action completionCallback)
        {
            return TryHoldAction(action, duration, interruptible, completionCallback, false);
        }

        public bool TryPreemptWithAction(ActionType action, float duration, bool interruptible, Action completionCallback)
        {
            return TryHoldAction(action, duration, interruptible, completionCallback, true);
        }

        public void InterruptAction()
        {
            if (AreFree) return;

            Log.Info($"Hands action {CurrentAction} of entity {name} interrupted");
            FreeHands();
            actionCompletionCallback = null;
        }

        public void ShooterUpdate()
        {
            FinishActionIfDue();
        }

        private bool TryHoldAction(ActionType action, float duration, bool interruptible, Action completionCallback, bool preempting)
        {
            string refusal = FindRefusal(action, duration, preempting);
            if (refusal != null)
            {
                Log.Debug($"Entity {name} can not start {action}, reason: {refusal}");
                return false;
            }

            if (!AreFree) Log.Info($"Hands action {CurrentAction} of entity {name} preempted by {action}");

            StartAction(action, duration, interruptible, completionCallback);
            Performed?.Invoke(action, duration);
            return true;
        }

        private string FindRefusal(ActionType action, float duration, bool preempting)
        {
            if (!health.IsAlive) return DeadRefusal;

            if (NeedsHands(action))
            {
                FinishActionIfDue();
                if (!AreFree && !(preempting && actionInterruptible)) return BusyHandsRefusal;
            }

            foreach (Func<ActionType, float, bool> check in checks)
            {
                if (!check(action, duration)) return check.Target.GetType().Name;
            }

            return null;
        }

        private static bool NeedsHands(ActionType action)
        {
            switch (action)
            {
                case ActionType.Shoot:
                case ActionType.Reload:
                case ActionType.Sleep:
                    return true;
                default:
                    return false;
            }
        }

        // Checked on every request too, not only in Update: an action that ran out this frame frees the hands whatever
        // ran first in the frame, so a shot follows the same interval as the shooter's ShotCadence
        private void FinishActionIfDue()
        {
            if (AreFree || Time.time - actionStartedAt < actionDuration) return;

            Action finishedActionCallback = actionCompletionCallback;
            FreeHands();
            actionCompletionCallback = null;
            finishedActionCallback?.Invoke();
        }

        private void StartAction(ActionType action, float duration, bool interruptible, Action completionCallback)
        {
            work.Value = new Work { Action = action, SequenceNumber = work.Value.SequenceNumber + 1, Duration = duration };
            actionStartedAt = Time.time;
            actionDuration = duration;
            actionInterruptible = interruptible;
            actionCompletionCallback = completionCallback;
            ActionStarted?.Invoke(action, duration);
        }

        private void FreeHands()
        {
            work.Value = new Work { Action = ActionType.None, SequenceNumber = work.Value.SequenceNumber + 1 };
        }

        private void HandleWorkReplicated(Work previousWork, Work currentWork)
        {
            if (currentWork.Action == ActionType.None) return;

            ActionStarted?.Invoke(currentWork.Action, currentWork.Duration);
        }

        public struct Work : INetworkSerializeByMemcpy, IEquatable<Work>
        {
            public ActionType Action;
            public int SequenceNumber;
            public float Duration;

            public bool Equals(Work other)
            {
                return Action == other.Action && SequenceNumber == other.SequenceNumber && Duration.Equals(other.Duration);
            }
        }
    }
}
