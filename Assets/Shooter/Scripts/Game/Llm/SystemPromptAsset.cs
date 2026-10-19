using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Llm/System Prompt", fileName = "SystemPrompt")]
    public class SystemPromptAsset : Asset
    {
        [SerializeField] [TextArea(5, 20)] private string content;

        public string Content => content;
    }
}
