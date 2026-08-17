using UnityEngine;

namespace Shooter.Game.AI.Healer
{
    [RequireComponent(typeof(AIHealer))]
    public class AISettingHealer : AISetting
    {
        private AIHealer healer;

        public override string Name => "healer";
        public override string Range => "False - True";

        public override string Description => @"
            If enabled, your character will automatically heal using healing items from your inventory when injured.
            It is strongly advised against disabling this setting, as doing so would prevent your character from timely health recovery after sustaining a severe injury.
         ";

        private void Awake()
        {
            healer = GetComponent<AIHealer>();
        }

        public override void Set(string content)
        {
            bool value = bool.Parse(content);
            healer.enabled = value;
        }

        public override string Get()
        {
            return healer.enabled.ToString();
        }
    }
}
