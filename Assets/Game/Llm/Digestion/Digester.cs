using System.Text;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class Digester : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public static Digester Current { get; private set; }

        private void Awake()
        {
            if (Current != null)
            {
                Log.Error("Singleton class has more than one instance");
            }
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public string Of(GameObject entity, DigestionDetail detail)
        {
            return entity == null ? null : Of(entity.GetComponent<MainDigestible>(), detail);
        }

        public string Of(MainDigestible entity, DigestionDetail detail)
        {
            if (entity == null) return null;

            var head = new StringBuilder();
            var body = new StringBuilder();

            foreach (IDigestible part in entity.Parts)
            {
                string said = part.Digest(detail);
                if (string.IsNullOrWhiteSpace(said)) continue;

                bool heading = part.Priority >= DigestionPriority.Place;

                foreach (string line in said.Trim().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (heading)
                    {
                        if (head.Length > 0) head.Append(' ');
                        head.Append(line.Trim());
                        heading = false;
                        continue;
                    }

                    body.Append("\n  ").Append(line.TrimEnd());
                }
            }

            if (head.Length == 0) return null;

            return head.Append(body).ToString();
        }
    }
}
