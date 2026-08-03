using UnityEngine;

namespace Shooter.Game.Llm.Knowledge
{
    [CreateAssetMenu(menuName = "Shooter-Llm/KnowledgeSpec", fileName = "KnowledgeSpec")]
    public class KnowledgeSpec : Spec
    {
        [SerializeField] [TextArea(3, 10)] private string content;

        public string Content => content;
    }
}
