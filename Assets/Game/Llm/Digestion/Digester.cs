using System.Text;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.AI;

namespace Shooter.Game.Llm
{
    [DefaultExecutionOrder(ExecutionOrder.Service)]
    public class Digester : MonoBehaviour
    {
        private const float FloorReach = 2f;
        private const float FloorAbove = 0.5f;

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
            return entity == null ? null : Block(entity.GetComponent<MainDigestible>(), detail, null);
        }

        public string Seen(MainDigestible entity, DigestionDetail detail, Vector3 feet)
        {
            return Block(entity, detail, feet);
        }

        private string Block(MainDigestible entity, DigestionDetail detail, Vector3? feet)
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
                        if (feet != null) digest.Append(" (").Append(Whereabouts(entity, feet.Value)).Append(")");
                        continue;
                    }

                    digest.Append("\n  ").Append(line.TrimEnd());
                }
            }

            if (digest.Length == 0) return null;

            GameObjectRuntimeId id = entity.GetComponent<GameObjectRuntimeId>();
            if (id == null) return digest.ToString();

            return "[ID " + id.Value + "] " + digest;
        }

        private string Whereabouts(MainDigestible entity, Vector3 feet)
        {
            Vector3 offset = entity.transform.position - feet;
            float rise = Level(entity.transform.position) - Level(feet);
            offset.y = float.IsNaN(rise) ? 0f : rise;
            return Cardinal.Whereabouts(offset);
        }

        private static float Level(Vector3 position)
        {
            if (!AgentMovement.NearestGround(position, FloorReach, out NavMeshHit floor)) return float.NaN;
            if (floor.position.y > position.y + FloorAbove) return float.NaN;

            return floor.position.y;
        }
    }
}
