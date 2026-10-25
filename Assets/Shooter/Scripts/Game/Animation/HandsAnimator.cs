using System.Collections;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Hands))]
    [RequireComponent(typeof(Skin))]
    public class HandsAnimator : ShooterBehaviour
    {
        private const string ActionLayer = "Armed";
        private const float ActionFade = 0.05f;
        private static readonly int ShootingState = Animator.StringToHash("Shooting");
        private static readonly int ReloadingState = Animator.StringToHash("Reloading");
        private static readonly int ActionSpeedParameter = Animator.StringToHash("ActionSpeed");

        private Hands hands;
        private Skin skin;

        protected override void ShooterAwake()
        {
            hands = GetComponent<Hands>();
            skin = GetComponent<Skin>();
        }

        protected override void ShooterOnEnable()
        {
            hands.ActionStarted += PlayAction;
        }

        protected override void ShooterOnDisable()
        {
            hands.ActionStarted -= PlayAction;
        }

        private void PlayAction(ActionType action, float duration)
        {
            if (skin.ModelInstance == null) return;

            Animator animator = skin.ModelInstance.GetComponent<Animator>();
            if (animator == null) return;

            int layer = animator.GetLayerIndex(ActionLayer);
            if (layer < 0) return;

            int state = GetState(action);
            if (!animator.HasState(layer, state)) return;

            animator.CrossFadeInFixedTime(state, ActionFade, layer);
            StartCoroutine(FitActionSpeedToDuration(animator, layer, duration));
        }

        private static int GetState(ActionType action)
        {
            switch (action)
            {
                case ActionType.Shoot:
                    return ShootingState;
                case ActionType.Reload:
                    return ReloadingState;
                default:
                    return 0;
            }
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
    }
}
