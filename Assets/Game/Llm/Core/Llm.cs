using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.World;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(LlmHistory))]
    [RequireComponent(typeof(LlmWaiting))]
    public class Llm : MonoBehaviour, IMortal
    {
        private const string RetellingDemand =
            "Your story became too long and MUST be retold. Call rewrite_summary with a full retelling of your whole story: the call itself will remain as the only story of your life, everything older will be erased, and anything you leave out of the retelling is lost FOREVER. Keep all the details important for the continuity of your life and deep communication. Keep your voice exactly as it is now: your manner of speech, your verbal quirks, a few literal sample phrases. Weave what you know and what you lived through into one story. Pay special attention to the most recent events and to the questions you have not answered yet: they must survive in full detail. Compress to at most half the length.";

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
        private LlmWaiting waiting;

        private void Awake()
        {
            history = GetComponent<LlmHistory>();
            waiting = GetComponent<LlmWaiting>();
            abilities = GetComponents<LlmTool>();
            entityName = this.NameOf();
        }

        private void OnDestroy()
        {
            life.Cancel();
        }

        public void Died()
        {
            life.Cancel();
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

        public void Notice(string line)
        {
            history.Arrive(new LlmMessage { Role = LlmRole.User, Content = line });
        }

        public void Listen(long wandererId, string message, Action<string> onAnswer)
        {
            waiting.Listen(wandererId, message, onAnswer);
        }

        public void Forget(long wandererId)
        {
            waiting.Forget(wandererId);
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

            try
            {
                bool retelling = history.Overflowing;

                if (history.Count == 0) Begin();

                history.Seen();
                history.Append(new LlmMessage { Role = LlmRole.User, Content = Observation() });
                history.Snapshot();

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
                            { Role = LlmRole.Tool, ToolCallId = call.Id, Content = Execute(tools, call) });
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
                gate.Release();
            }
        }

        private string Observation()
        {
            var seen = new StringBuilder();
            seen.Append('[').Append(Time()).Append(']');

            if (history.Overflowing) seen.Append('\n').Append(RetellingDemand);

            return seen.ToString();
        }

        private static LlmConfig Fitting(List<LlmTool> tools)
        {
            return tools.Any(tool => tool.Level == LlmLevel.Max)
                ? Config.Read().Server.LlmMax
                : Config.Read().Server.LlmBase;
        }

        private string Execute(IReadOnlyList<ILlmTool> tools, LlmToolCall call)
        {
            ILlmTool tool = tools.FirstOrDefault(known => known.Name == call.Name);
            if (tool == null) return $"There is no tool named {call.Name}";

            try
            {
                return tool.Execute(call.Arguments);
            }
            catch (Exception e)
            {
                Log.Warn($"Entity {entityName} broke the tool {call.Name} {call.Arguments}: {e.Message}");
                return $"The action failed: {e.Message}";
            }
        }

        private string Time()
        {
            return Clock.Current.DateTime();
        }
    }
}
