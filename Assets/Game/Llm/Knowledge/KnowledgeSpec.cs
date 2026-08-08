using UnityEngine;

namespace Shooter.Game.Llm.Knowledge
{
    [CreateAssetMenu(menuName = "Shooter-Llm/KnowledgeSpec", fileName = "KnowledgeSpec")]
    public class KnowledgeSpec : Spec
    {

        [SerializeField] [TextArea(5, 20)] private string content;

        public string Content => content;
    }
}
