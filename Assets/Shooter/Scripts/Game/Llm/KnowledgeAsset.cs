using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Llm/Knowledge", fileName = "Knowledge")]
    public class KnowledgeAsset : Asset
    {
        [SerializeField] [TextArea(5, 20)] private string content;

        public string Content => content;
    }
}
