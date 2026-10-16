using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter-Llm/System Prompt Spec", fileName = "SystemPrompt")]
    public class SystemPromptSpec : ScriptableObject
    {
        [SerializeField] [TextArea(5, 20)] private string content;

        public string Content => content;
    }
}
