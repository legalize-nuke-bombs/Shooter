using System;
using System.Collections;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Skin))]
    public class Hands : NetworkBehaviour, IMortal, IDigestible
    {
        private const string ActionLayer = "Armed";
        private const float ActionFade = 0.05f;
        private static readonly Journal Log = Logs.Here();
        private static readonly int[] ActionStates = BuildActionStates();
        private static readonly int ActionSpeedParameter = Animator.StringToHash("ActionSpeed");

        private readonly NetworkVariable<Work> work = new();

        private Action completionCallback;
        private bool interruptible;
        private float remainingSeconds;
        private Skin skin;

        public HandsAction CurrentAction => work.Value.Action;

        public bool AreFree => CurrentAction == HandsAction.None;

        public string Digest(DigestionDetail detail)
        {
            return AreFree ? null : "Busy: " + CurrentAction;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        public void HandleDeath()
        {
            InterruptAction();
        }

        private void Awake()
        {
            skin = GetComponent<Skin>();
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            work.OnValueChanged += PlayActionAnimation;

            if (!IsServer) return;

            enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            work.OnValueChanged -= PlayActionAnimation;

            if (!IsServer) return;

            enabled = false;
        }

        public bool TryStartAction(HandsAction action, float duration, bool interruptible, Action completionCallback)
        {
            if (!AreFree) return false;

            StartAction(action, duration, interruptible, completionCallback);
            return true;
        }

        public bool TryPreemptWithAction(HandsAction action, float duration, bool interruptible, Action completionCallback)
        {
            if (!AreFree && !this.interruptible) return false;
            if (!AreFree) Log.Info($"Hands action {CurrentAction} of entity {name} preempted by {action}");

            StartAction(action, duration, interruptible, completionCallback);
            return true;
        }

        public void InterruptAction()
        {
            if (AreFree) return;

            Log.Info($"Hands action {CurrentAction} of entity {name} interrupted");
            FreeHands();
            completionCallback = null;
            remainingSeconds = 0f;
        }

        private void Update()
        {
            if (AreFree) return;

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds > 0f) return;

            Action finishedActionCallback = completionCallback;
            FreeHands();
            completionCallback = null;
            finishedActionCallback?.Invoke();
        }

        private void StartAction(HandsAction action, float duration, bool interruptible, Action completionCallback)
        {
            work.Value = new Work { Action = action, SequenceNumber = work.Value.SequenceNumber + 1, Duration = duration };
            remainingSeconds = duration;
            this.interruptible = interruptible;
            this.completionCallback = completionCallback;
        }

        private void FreeHands()
        {
            work.Value = new Work { Action = HandsAction.None, SequenceNumber = work.Value.SequenceNumber + 1 };
        }

        private void PlayActionAnimation(Work previousWork, Work currentWork)
        {
            if (currentWork.Action == HandsAction.None) return;
            if (skin.ModelInstance == null) return;

            Animator animator = skin.ModelInstance.GetComponent<Animator>();
            if (animator == null) return;

            int layer = animator.GetLayerIndex(ActionLayer);
            if (layer < 0) return;

            int state = ActionStates[(int)currentWork.Action];
            if (!animator.HasState(layer, state)) return;

            animator.CrossFadeInFixedTime(state, ActionFade, layer);
            StartCoroutine(FitActionSpeedToDuration(animator, layer, currentWork.Duration));
        }

        private static IEnumerator FitActionSpeedToDuration(Animator animator, int layer, float duration)
        {
            yield return null;

            if (animator == null || duration <= 0f) yield break;

            AnimatorClipInfo[] clips = animator.GetNextAnimatorClipInfo(layer);
            if (clips.Length == 0) clips = animator.GetCurrentAnimatorClipInfo(layer);
            if (clips.Length == 0 || clips[0].clip == null) yield break;

            animator.SetFloat(ActionSpeedParameter, clips[0].clip.length / duration);
        }

        private static int[] BuildActionStates()
        {
            string[] actionNames = Enum.GetNames(typeof(HandsAction));
            int[] states = new int[actionNames.Length];
            for (int i = 0; i < actionNames.Length; i++)
            {
                states[i] = Animator.StringToHash(actionNames[i]);
            }

            return states;
        }

        public struct Work : INetworkSerializeByMemcpy, IEquatable<Work>
        {
            public HandsAction Action;
            public int SequenceNumber;
            public float Duration;

            public bool Equals(Work other)
            {
                return Action == other.Action && SequenceNumber == other.SequenceNumber && Duration.Equals(other.Duration);
            }
        }
    }
}
