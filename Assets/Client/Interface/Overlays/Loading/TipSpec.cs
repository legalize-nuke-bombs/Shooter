using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Client.Interface
{
    [CreateAssetMenu(menuName = "Shooter/Tip", fileName = "Tip")]
    public class TipSpec : Spec
    {
        [SerializeField] [TextArea] private string text;

        public string Text => text;
    }
}
