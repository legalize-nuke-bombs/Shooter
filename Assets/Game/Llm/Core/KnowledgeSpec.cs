using UnityEngine;
using Shooter.Game.Core;

namespace Shooter.Game.Llm
{
    [CreateAssetMenu(menuName = "Shooter-Llm/KnowledgeSpec", fileName = "KnowledgeSpec")]
    public class KnowledgeSpec : Spec
    {

        [SerializeField] [TextArea(5, 20)] private string content;

        public string Content => content;
    }
}
