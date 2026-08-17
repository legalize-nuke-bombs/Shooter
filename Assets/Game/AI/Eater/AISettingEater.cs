using UnityEngine;

namespace Shooter.Game.AI.Eater
{
    [RequireComponent(typeof(AIEater))]
    public class AISettingEater : AISetting
    {
        private AIEater eater;

        private void Awake()
        {
            eater = GetComponent<AIEater>();
        }

        public override string Name => "eater";
        public override string Range => "False - True";

        public override string Description => @"
            If enabled, your character will automatically eat edible items from your inventory when hungry.
         ";

        public override void Set(string content)
        {
            bool value = bool.Parse(content);
            eater.enabled = value;
        }

        public override string Get()
        {
            return eater.enabled.ToString();
        }
    }
}
