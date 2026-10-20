using System;
using System.Collections;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Skin))]
    [RequireComponent(typeof(Health))]
    public class Hands : ShooterNetworkBehaviour, IDigestible
    {
        private const string ActionLayer = "Armed";
        private const float ActionFade = 0.05f;
        private static readonly Journal Log = Logs.Here();
        private static readonly int[] ActionStates = BuildActionStates();
        private static readonly int ActionSpeedParameter = Animator.StringToHash("ActionSpeed");

        private readonly NetworkVariable<Work> work = new();

        private Action actionCompletionCallback;
        private bool actionInterruptible;
        private float actionStartedAt;
        private float actionDuration;
        private Health health;
        private Skin skin;

        public HandsAction CurrentAction => work.Value.Action;

        public bool AreFree => CurrentAction == HandsAction.None;

        public string Digest(DigestionDetail detail)
        {
            return AreFree ? null : "Busy: " + CurrentAction;
        }

        public DigestionPriority Priority => DigestionPriority.Low;

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
            skin = GetComponent<Skin>();
            enabled = false;
        }

        private void OnEnable()
        {
            health.Died += InterruptAction;
        }

        private void OnDisable()
        {
            health.Died -= InterruptAction;
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
            FinishActionIfDue();
            if (!AreFree) return false;

            StartAction(action, duration, interruptible, completionCallback);
            return true;
        }

        public bool TryPreemptWithAction(HandsAction action, float duration, bool interruptible, Action completionCallback)
        {
            FinishActionIfDue();
            if (!AreFree && !actionInterruptible) return false;
            if (!AreFree) Log.Info($"Hands action {CurrentAction} of entity {name} preempted by {action}");

            StartAction(action, duration, interruptible, completionCallback);
            return true;
        }

        public void InterruptAction()
        {
            if (AreFree) return;

            Log.Info($"Hands action {CurrentAction} of entity {name} interrupted");
            FreeHands();
            actionCompletionCallback = null;
        }

        private void Update()
        {
            FinishActionIfDue();
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

        private void StartAction(HandsAction action, float duration, bool interruptible, Action completionCallback)
        {
            work.Value = new Work { Action = action, SequenceNumber = work.Value.SequenceNumber + 1, Duration = duration };
            actionStartedAt = Time.time;
            actionDuration = duration;
            actionInterruptible = interruptible;
            actionCompletionCallback = completionCallback;
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
