using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Aiming;
using Shooter.Client.Hud.Sleeping;

namespace Shooter.Client.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField] private Aim aim;
        [SerializeField] private SleepSense sleepSense;

        private void Awake()
        {
            if (Application.isBatchMode) enabled = false;
        }

        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.Add(new Crosshair());
            root.Add(new ClockLabel(font));
            root.Add(new TargetNameLabel(font, aim));
            root.Add(new SleepOverlay(sleepSense));
            root.Add(new SleepHintLabel(font, sleepSense));
        }
    }
}
