using System.Text;
using UnityEngine;

namespace Shooter.Client.Interface.Naming
{
    [CreateAssetMenu(menuName = "Shooter/Glitch Name", fileName = "GlitchName")]
    public sealed class GlitchNameSpec : NameSpec
    {
        [SerializeField] private string alphabet = "017XREVID#$@%?!&";

        [SerializeField] private string[] messages = { "I_SEE_YOU", "WAKE_UP", "THE_END_IS_NEAR" };

        [SerializeField] [Range(0f, 1f)] private float messageChance = 0.15f;

        [SerializeField] private int shortest = 10;

        [SerializeField] private int longest = 20;

        public override string Text()
        {
            if (messages.Length > 0 && Random.value < messageChance) return messages[Random.Range(0, messages.Length)];

            if (string.IsNullOrEmpty(alphabet)) return string.Empty;

            int length = Random.Range(shortest, longest + 1);
            var noise = new StringBuilder(length);

            for (int written = 0; written < length; written++) noise.Append(alphabet[Random.Range(0, alphabet.Length)]);

            return noise.ToString();
        }
    }
}
