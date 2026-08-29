using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmHistory))]
    [RequireComponent(typeof(LlmPendingTable))]
    public class Llm : MonoBehaviour, IMortal
    {
        private const string ClearHeadDemand =
            "Your head is too full and MUST be cleared: only your notes will survive, ALL THE REST is lost forever. Bring your notes up to date in full detail, including the very latest context - what is happening right now and where you left off. Then call clear_head.";

        private const string StoryHeader =
            "THE STORY OF YOUR LIFE SO FAR (it will be erased by the first clearing of your head and will never return - you MUST save ALL of it into your notes at once, in full detail):\n";

        private const string StampFormat = "yyyy.MM.dd HH:mm:ss";

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SystemPromptSpec[] systemPrompts;
        [SerializeField] [TextArea(5, 20)] private string character;
        [SerializeField] private KnowledgeSpec[] knowledges;
        [SerializeField] private float failureCooldown = 2.5f;
        [SerializeField] private int maxToolRounds = 10;

        private readonly SemaphoreSlim gate = new(1, 1);
        private readonly CancellationTokenSource life = new();
        private LlmTool[] abilities;
        private string entityName;

        private LlmHistory history;
        private float retryBlockedUntil;
        private LlmPendingTable table;

        public bool Busy { get; private set; }

        public event Action<long, string> Answered;

        private void Awake()
        {
            history = GetComponent<LlmHistory>();
            table = GetComponent<LlmPendingTable>();
            abilities = GetComponents<LlmTool>();
            entityName = name;
        }

        private void OnDestroy()
        {
            life.Cancel();
        }

        public void Died()
        {
            life.Cancel();
            Abandon(table.Ids());
        }

        private void Begin()
        {
            if (String.IsNullOrEmpty(character))
            {
                Log.Warn($"Entity {name} does not have character!");
            }

            string start = (character + "\n" + Knowledge()).Trim('\n');
            if (start.Length == 0) return;

            history.Append(new LlmMessage { Role = LlmRole.User, Content = StoryHeader + start });
        }

        private string SystemPrompt()
        {
            var systemPrompt = new StringBuilder();
            foreach (SystemPromptSpec sp in systemPrompts)
            {
                if (sp == null)
                {
                    Log.Warn($"Entity {entityName} has an empty slot among its {systemPrompts.Length} prompts");
                    continue;
                }

                systemPrompt.Append(sp.Content).Append('\n');
            }

            return systemPrompt.ToString();
        }

        private string Knowledge()
        {
            var known = new StringBuilder();
            foreach (KnowledgeSpec knowledge in knowledges)
            {
                if (knowledge == null)
                {
                    Log.Warn($"Entity {entityName} has an empty slot among its {knowledges.Length} knowledges");
                    continue;
                }

                known.Append(knowledge.Content).Append('\n');
            }

            return known.ToString();
        }

        public void Notice(string line, bool urgent, long? askerId = null)
        {
            history.Arrive(new LlmMessage { Role = LlmRole.User, Content = line }, urgent);
            if (askerId != null) table.Mark(askerId.Value);
        }

        public bool Answer(long wandererId, string text)
        {
            if (!table.Clear(wandererId)) return false;

            Answered?.Invoke(wandererId, text);
            return true;
        }

        public LlmStatus Status()
        {
            return new LlmStatus
            {
                PendingCompact = history.Overflowing,
                PendingMail = history.Unseen
            };
        }

        public async Task<bool> Tick()
        {
            if (life.IsCancellationRequested || UnityEngine.Time.time < retryBlockedUntil) return false;

            bool entered = await gate.WaitAsync(0, life.Token);

            if (!entered) return false;

            List<long> asked = table.Ids();
            Busy = true;
            bool ticked = false;

            try
            {
                ticked = await Think(asked);
            }
            catch (OperationCanceledException)
            {
                Log.Info($"Entity {entityName} dropped its request, the entity is gone");
            }
            catch (Exception e)
            {
                retryBlockedUntil = UnityEngine.Time.time + failureCooldown;
                Log.Warn($"Entity {entityName} failed to respond, next attempt in {failureCooldown} s: {e}");
            }

            Abandon(asked);
            Busy = false;
            gate.Release();

            return ticked;
        }

        private async Task<bool> Think(List<long> asked)
        {
            if (String.IsNullOrEmpty(Config.Read().Server.LlmBase.Provider)) return false;

            bool clearing = history.Overflowing;

            if (history.Count == 0) Begin();

            history.Seen();
            history.Append(new LlmMessage { Role = LlmRole.User, Content = Observation(asked) });
            var context = new LlmCallContext { PromptedCount = history.Count };

            var selected = abilities.Where(ability => ability.Available).ToList();
            LlmConfig config = Fitting(selected);
            var tools = selected.Cast<ILlmTool>().ToList();

            Log.Info(
                $"Entity {entityName} is asking {config.Model}, history {history.Count} messages / {history.Size} chars");

            for (int round = 0; round < maxToolRounds; round++)
            {
                LlmTurn turn = await LlmProvider.Request(
                    config,
                    $"{entityName}-{(clearing ? "compact" : "live")}",
                    SystemPrompt(),
                    history.Messages,
                    tools,
                    life.Token
                );
                life.Token.ThrowIfCancellationRequested();

                history.Append(new LlmMessage
                    { Role = LlmRole.Assistant, Content = turn.Content, ToolCalls = turn.ToolCalls });

                if (!turn.CallsTools) break;

                foreach (LlmToolCall call in turn.ToolCalls)
                    history.Append(new LlmMessage
                        { Role = LlmRole.Tool, ToolCallId = call.Id, Content = Execute(tools, call, context) });
            }

            if (clearing && history.Overflowing)
                throw new LlmException("The story is still overflowing after the clearing tick");

            return true;
        }

        private void Abandon(List<long> asked)
        {
            foreach (long id in asked)
            {
                if (!table.Clear(id)) continue;

                Log.Warn($"Entity {entityName} has not answered wanderer {id}, the fallback is said instead");

                try
                {
                    Answered?.Invoke(id, null);
                }
                catch (Exception e)
                {
                    Log.Warn($"Entity {entityName} failed to deliver the fallback to wanderer {id}: {e.Message}");
                }
            }
        }

        private string Observation(List<long> asked)
        {
            var seen = new StringBuilder();
            seen.Append('[').Append(Stamp()).Append(']');

            if (history.Overflowing) seen.Append('\n').Append(ClearHeadDemand);

            if (asked.Count > 0)
                seen.Append('\n').Append("You MUST answer the waiting wanderer(s) RIGHT NOW using the say_to_wanderer tool: ")
                    .Append(string.Join(", ", asked)).Append('.');

            return seen.ToString();
        }

        private static LlmConfig Fitting(List<LlmTool> tools)
        {
            return tools.Any(tool => tool.Level == LlmLevel.Max)
                ? Config.Read().Server.LlmMax
                : Config.Read().Server.LlmBase;
        }

        private string Execute(IReadOnlyList<ILlmTool> tools, LlmToolCall call, LlmCallContext context)
        {
            ILlmTool tool = tools.FirstOrDefault(known => known.Name == call.Name);
            if (tool == null) return $"There is no tool named {call.Name}";

            try
            {
                return tool.Execute(call.Arguments, context);
            }
            catch (Exception e)
            {
                Log.Warn($"Entity {entityName} broke the tool {call.Name} {call.Arguments}: {e.Message}");
                return $"The action failed: {e.Message}";
            }
        }

        public static string Stamp()
        {
            return Clock.Current.Now.ToString(StampFormat, CultureInfo.InvariantCulture);
        }
    }
}
