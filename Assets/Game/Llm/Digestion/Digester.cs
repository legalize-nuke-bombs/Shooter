using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shooter.Game.Core;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public class Digester : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        public string Of(GameObject entity, DigestionDetail detail)
        {
            return Block(entity, detail, null);
        }

        public string Seen(GameObject entity, DigestionDetail detail, Transform eyes)
        {
            return Block(entity, detail, eyes);
        }

        private string Block(GameObject entity, DigestionDetail detail, Transform eyes)
        {
            IEnumerable<IDigestible> parts = entity.GetComponents<IDigestible>()
                .OrderByDescending(part => part.Priority);

            var digest = new StringBuilder();

            foreach (IDigestible part in parts)
            {
                string said = part.Digest(detail);
                if (string.IsNullOrWhiteSpace(said)) continue;

                foreach (string line in said.Trim().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (digest.Length == 0)
                    {
                        digest.Append(line.TrimEnd());
                        if (eyes != null) digest.Append(" (").Append(Whereabouts(entity, eyes)).Append(")");
                        continue;
                    }

                    digest.Append("\n  ").Append(line.TrimEnd());
                }
            }

            if (digest.Length == 0)
            {
                return null;
            }

            PersistentId id = entity.GetComponent<PersistentId>();
            if (id == null)
            {
                return digest.ToString();
            }

            return "[ID " + id.Value + "] " + digest;
        }

        public DigestibleSize Size(GameObject entity)
        {
            DigestibleSize? size = null;

            IDigestible[] digestibles = entity.GetComponents<IDigestible>();
            foreach (IDigestible digestible in digestibles)
            {
                DigestibleSize? digestibleSize = digestible.Size;
                if (digestibleSize == null)
                {
                    continue;
                }

                if (size == null)
                {
                    size = digestibleSize.Value;
                }
                else
                {
                    if (digestibleSize.Value > size.Value)
                    {
                        size = digestibleSize;
                    }
                }
            }

            if (size == null)
            {
                Log.Warn($"Entity {entity.name} has no distible size overrides, using the small");
                return DigestibleSize.Small;
            }

            return size.Value;
        }

        private string Whereabouts(GameObject entity, Transform eyes)
        {
            Vector3 offset = entity.transform.position - eyes.position;
            return Mathf.RoundToInt(offset.magnitude) + " m, " + Cardinal.Side(offset);
        }
    }
}
