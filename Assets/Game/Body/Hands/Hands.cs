using System;
using Shooter.Game.Core;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Skin))]
    public class Hands : NetworkBehaviour, IMortal, IDigestible
    {
        private const string ActionLayer = "Armed";
        private const float ActionFade = 0.05f;
        private static readonly Journal Log = Logs.Here();
        private static readonly int[] ActionStates = BuildActionStates();
        private static readonly int ActionSpeed = Animator.StringToHash("ActionSpeed");

        private readonly NetworkVariable<Work> work = new();

        private Action complete;
        private bool interruptible;
        private float remaining;
        private Skin skin;

        public HandsAction Action => work.Value.Action;

        public bool Free => Action == HandsAction.None;

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
            skin = GetComponent<Skin>();
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            work.OnValueChanged += Show;

            if (!IsServer) return;

            enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            work.OnValueChanged -= Show;

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
            Drop();
            complete = null;
            remaining = 0f;
        }

        private void Update()
        {
            if (Free) return;

            remaining -= Time.deltaTime;
            if (remaining > 0f) return;

            Action finished = complete;
            Drop();
            complete = null;
            finished?.Invoke();
        }

        private void Take(HandsAction wanted, float duration, bool interruptible, Action complete)
        {
            work.Value = new Work { Action = wanted, Round = work.Value.Round + 1, Duration = duration };
            remaining = duration;
            this.interruptible = interruptible;
            this.complete = complete;
        }

        private void Drop()
        {
            work.Value = new Work { Action = HandsAction.None, Round = work.Value.Round + 1 };
        }

        private void Show(Work before, Work now)
        {
            if (now.Action == HandsAction.None) return;
            if (skin.Flesh == null) return;

            Animator animator = skin.Flesh.GetComponent<Animator>();
            if (animator == null) return;

            int layer = animator.GetLayerIndex(ActionLayer);
            if (layer < 0) return;

            int state = ActionStates[(int)now.Action];
            if (!animator.HasState(layer, state)) return;

            animator.CrossFadeInFixedTime(state, ActionFade, layer);
            StartCoroutine(Pace(animator, layer, now.Duration));
        }

        private static System.Collections.IEnumerator Pace(Animator animator, int layer, float duration)
        {
            yield return null;

            if (animator == null || duration <= 0f) yield break;

            AnimatorClipInfo[] clips = animator.GetNextAnimatorClipInfo(layer);
            if (clips.Length == 0) clips = animator.GetCurrentAnimatorClipInfo(layer);
            if (clips.Length == 0 || clips[0].clip == null) yield break;

            animator.SetFloat(ActionSpeed, clips[0].clip.length / duration);
        }

        private static int[] BuildActionStates()
        {
            string[] names = Enum.GetNames(typeof(HandsAction));
            var states = new int[names.Length];
            for (int i = 0; i < names.Length; i++) states[i] = Animator.StringToHash(names[i]);
            return states;
        }

        public struct Work : INetworkSerializeByMemcpy, IEquatable<Work>
        {
            public HandsAction Action;
            public int Round;
            public float Duration;

            public bool Equals(Work other)
            {
                return Action == other.Action && Round == other.Round && Duration.Equals(other.Duration);
            }
        }
    }
}
