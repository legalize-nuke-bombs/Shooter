using UnityEngine;

namespace Shooter.Game.Llm.Knowledge
{
    [CreateAssetMenu(menuName = "Shooter-Llm/KnowledgeSpec", fileName = "KnowledgeSpec")]
    public class KnowledgeSpec : Spec
    {

        [SerializeField] [TextArea(5, 20)] private string content;
        [SerializeField] private KnowledgeType type;

        public string Content => content;
        public KnowledgeType Type => type;
    }
}
