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
        private const string RetellingDemand =
            "Your story became too long and MUST be retold. Call rewrite_summary with a full retelling of your whole story: the call itself will remain as the only story of your life, everything older will be erased, and anything you leave out of the retelling is lost FOREVER. Keep all the details important for the continuity of your life and deep communication. Keep your voice exactly as it is now: your manner of speech, your verbal quirks, a few literal sample phrases. Weave what you know and what you lived through into one story. Pay special attention to the most recent events and to the questions you have not answered yet: they must survive in full detail. Compress to at most half the length.";

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
            string start = (character + "\n" + Knowledge()).Trim('\n');
            if (start.Length == 0) return;

            history.Append(new LlmMessage
                { Role = LlmRole.User, Content = "THE STORY OF YOUR LIFE SO FAR:\n" + start });
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
            if (String.IsNullOrEmpty(Config.Read().Server.LlmBase.Provider)) return false;

            bool entered = await gate.WaitAsync(0, life.Token);

            if (!entered) return false;

            List<long> asked = table.Ids();
            Busy = true;

            try
            {
                bool retelling = history.Overflowing;

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
                        $"{entityName}-{(retelling ? "compact" : "live")}",
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

                if (retelling && history.Overflowing)
                    throw new LlmException("The story is still overflowing after the retelling tick");

                return true;
            }
            catch (OperationCanceledException)
            {
                Log.Info($"Entity {entityName} dropped its request, the entity is gone");
                return false;
            }
            catch (Exception e)
            {
                retryBlockedUntil = UnityEngine.Time.time + failureCooldown;
                Log.Warn($"Entity {entityName} failed to respond, next attempt in {failureCooldown} s: {e}");
                return false;
            }
            finally
            {
                Abandon(asked);
                Busy = false;
                gate.Release();
            }
        }

        private void Abandon(List<long> asked)
        {
            foreach (long id in asked)
            {
                if (!table.Clear(id)) continue;

                Log.Warn($"Entity {entityName} has not answered wanderer {id}, the fallback is said instead");
                Answered?.Invoke(id, null);
            }
        }

        private string Observation(List<long> asked)
        {
            var seen = new StringBuilder();
            seen.Append('[').Append(Stamp()).Append(']');

            if (history.Overflowing) seen.Append('\n').Append(RetellingDemand);

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
