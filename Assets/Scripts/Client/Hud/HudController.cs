using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Entities.Npcs;

namespace Shooter.Client.Hud
{
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        private const float NameReach = 12f;

        [SerializeField] private Font font;

        private Transform cameraTransform;
        private Label targetName;

        private void Awake()
        {
            if (Application.isBatchMode) enabled = false;
        }

        private void Start()
        {
            cameraTransform = Camera.main.transform;

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.Add(new Crosshair());
            targetName = BuildTargetName();
            root.Add(targetName);
        }

        private Label BuildTargetName()
        {
            var label = new Label();
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.right = 0;
            label.style.top = Length.Percent(50);
            label.style.marginTop = 24;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.fontSize = 15;
            label.style.color = new Color(0.76f, 0.79f, 0.83f);
            label.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
            label.style.textShadow = new TextShadow
            {
                offset = Vector2.zero,
                blurRadius = 8f,
                color = new Color(0f, 0f, 0f, 0.9f)
            };
            label.style.display = DisplayStyle.None;
            return label;
        }

        private void Update()
        {
            string name = TargetName();
            bool targeted = !string.IsNullOrEmpty(name);
            targetName.style.display = targeted ? DisplayStyle.Flex : DisplayStyle.None;
            if (targeted) targetName.text = name;
        }

        private string TargetName()
        {
            if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, NameReach))
                return null;
            return hit.transform.TryGetComponent(out NpcBody npc) ? npc.Avatar.Name : null;
        }
    }
}
