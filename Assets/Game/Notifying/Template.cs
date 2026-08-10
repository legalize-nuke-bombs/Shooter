using System.Text;
using Shooter.Logging;

namespace Shooter.Game.Notifying
{
    public static class Template
    {
        private static readonly Journal Log = Logs.Here();

        private const char Opening = '{';
        private const char Closing = '}';

        public static string Filled(string pattern, Notification notification, INames names)
        {
            if (string.IsNullOrEmpty(pattern)) return string.Empty;

            var filled = new StringBuilder(pattern.Length);
            int index = 0;

            while (index < pattern.Length)
            {
                if (pattern[index] != Opening)
                {
                    filled.Append(pattern[index]);
                    index++;
                    continue;
                }

                int closing = pattern.IndexOf(Closing, index);

                if (closing < 0)
                {
                    filled.Append(pattern, index, pattern.Length - index);
                    break;
                }

                string name = pattern.Substring(index + 1, closing - index - 1);
                string value = notification.Of(name);

                if (value == null)
                {
                    Log.Warn($"Notification {notification.Spec} carries nothing under {name}, the line keeps the placeholder as it is");
                    filled.Append(pattern, index, closing - index + 1);
                }
                else
                {
                    filled.Append(names.Of(name, value));
                }

                index = closing + 1;
            }

            return filled.ToString();
        }
    }
}
