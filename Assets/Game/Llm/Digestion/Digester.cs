using System.Text;
using Shooter.Game.Core;
using Shooter.Game.World;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public class Digester : MonoBehaviour
    {
        public string Of(GameObject entity, DigestionDetail detail)
        {
            return entity == null ? null : Block(entity.GetComponent<MainDigestible>(), detail, null);
        }

        public string Seen(MainDigestible entity, DigestionDetail detail, Vector3 eyes)
        {
            return Block(entity, detail, eyes);
        }

        private string Block(MainDigestible entity, DigestionDetail detail, Vector3? eyes)
        {
            if (entity == null) return null;

            var digest = new StringBuilder();

            foreach (IDigestible part in entity.Parts)
            {
                string said = part.Digest(detail);
                if (string.IsNullOrWhiteSpace(said)) continue;

                foreach (string line in said.Trim().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (digest.Length == 0)
                    {
                        digest.Append(line.TrimEnd());
                        if (eyes != null) digest.Append(" (").Append(Whereabouts(entity, eyes.Value)).Append(")");
                        continue;
                    }

                    digest.Append("\n  ").Append(line.TrimEnd());
                }
            }

            if (digest.Length == 0) return null;

            PersistentId id = entity.Id;
            if (id == null) return digest.ToString();

            return "[ID " + id.Value + "] " + digest;
        }

        private string Whereabouts(MainDigestible entity, Vector3 eyes)
        {
            Vector3 offset = entity.transform.position - eyes;
            return Mathf.RoundToInt(offset.magnitude) + " m, " + Cardinal.Side(offset);
        }
    }
}
