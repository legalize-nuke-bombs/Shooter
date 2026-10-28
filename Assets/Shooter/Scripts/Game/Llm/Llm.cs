using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(LlmHistory))]
    [RequireComponent(typeof(Health))]
    public class Llm : ShooterComponent, ISaveableComponent
    {
        private const string ClearHeadDemand = "Your head is too full and MUST be cleared: only your notes will survive, ALL THE REST is lost forever. Bring your notes up to date in full detail, including the very latest context - what is happening right now and where you left off. Then call clear_head.";
        private const string ForcedClearingNotice = "Your head overflowed and has been cleared by force: your story so far is gone, only your notes remain. Your notes are your memory now - read the ones you need before you act or speak, start with the one about who you are.";
        private const string LifeStoryHeader = "YOU MUST INTRODUCE YOURSELF TO YOUR FRIENDS NOW. YOU MUST UPDATE RELATION TO YOUR FRIENDS NOW.\nTHE STORY OF YOUR LIFE SO FAR (it will be erased by the first clearing of your head and will never return - you MUST save ALL of it into your notes at once, in full detail):\n";
        private const string LifeStoryConflictHeader = "The world has migrated to a new version, and your initial life story has changed. Two versions are shown below – the previous one and the new one.\nYou must analyze the difference and update your notes RIGHT NOW (if there are changes; this message will be ERASED after the first clear_head tool call).\nYou MUST to retain all intermediate memories, including any changes in character (if any).\n";

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private SystemPromptAsset[] systemPrompts;
        [SerializeField] [TextArea(5, 20)] private string character;
        [SerializeField] private KnowledgeAsset[] knowledges;
        private string lifeStoryConflictPrompt = null; // The order in which component states are loaded from the save is not defined, so a conflict is registered during state loading and resolved in PostLoad, once all of them are loaded.
        [SerializeField] private float failureCooldown = 2.5f;
        [SerializeField] private int maxToolRounds = 10;

        [SerializeReference, SubclassSelector] private List<LlmTool> abilities;

        private readonly SemaphoreSlim tickSemaphore = new(1, 1);
        private readonly CancellationTokenSource lifetimeCancellation = new();
        private string entityName;

        private Health health;
        private LlmHistory llmHistory;
        private float retryBlockedUntil;

        public bool IsThinking { get; private set; }

        protected override void ShooterAwake()
        {
            health = GetComponent<Health>();
            llmHistory = GetComponent<LlmHistory>();
            entityName = name;
            foreach (LlmTool tool in abilities)
            {
                tool.AttachTo(gameObject);
            }
            AppendLifeStory();
        }

        protected override void ShooterOnEnable()
        {
            health.Died += EndLifetime;
        }

        protected override void ShooterOnDisable()
        {
            health.Died -= EndLifetime;
        }

        protected override void ShooterOnDestroy()
        {
            EndLifetime();
        }

        private void EndLifetime()
        {
            lifetimeCancellation.Cancel();
        }

        private string BuildLifeStory()
        {
            if (String.IsNullOrEmpty(character))
            {
                Log.Error($"Entity {name} does not have character!");
            }
            return (character + "\n" + JoinKnowledges()).Trim('\n');
        }

        private void AppendLifeStory()
        {
            llmHistory.AppendMessage(new LlmMessage { Role = LlmRole.User, Content = LifeStoryHeader + BuildLifeStory() });
        }

        private string BuildSystemPrompt()
        {
            StringBuilder systemPrompt = new();
            systemPrompt.Append("SYSTEM PROMPT BEGIN\n");
            foreach (SystemPromptAsset systemPromptAsset in systemPrompts)
            {
                if (systemPromptAsset == null)
                {
                    Log.Error($"Entity {entityName} has an empty slot among its {systemPrompts.Length} prompts");
                    continue;
                }

                systemPrompt.Append(systemPromptAsset.Content).Append('\n');
            }

            systemPrompt.Append("SYSTEM PROMPT END");
            return systemPrompt.ToString();
        }

        private string JoinKnowledges()
        {
            StringBuilder knowledge = new();
            foreach (KnowledgeAsset knowledgeAsset in knowledges)
            {
                if (knowledgeAsset == null)
                {
                    Log.Error($"Entity {entityName} has an empty slot among its {knowledges.Length} knowledges");
                    continue;
                }

                knowledge.Append(knowledgeAsset.Content).Append('\n');
            }

            return knowledge.ToString();
        }


        private struct SaveData
        {
            public string LifeStory { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData
            {
                LifeStory = BuildLifeStory(),
            };
        }
        public void LoadObject(SaveToken content)
        {
            SaveData sd = content.ToObject<SaveData>();
            if (sd.LifeStory != BuildLifeStory())
            {
                lifeStoryConflictPrompt = sd.LifeStory;
            }
        }
        public void PostLoad()
        {
            if (lifeStoryConflictPrompt == null) return;

            Log.Info($"Entity {entityName} life story conflict resolved");
            llmHistory.AppendMessage(new LlmMessage{Role = LlmRole.User, Content = LifeStoryConflictHeader + $"Old:\n{lifeStoryConflictPrompt}\nNew:\n{BuildLifeStory()}"});
            lifeStoryConflictPrompt = null;
        }


        public void AppendEvent(string eventText, bool isUrgent)
        {
            llmHistory.AppendEvent(new LlmMessage { Role = LlmRole.User, Content = eventText }, isUrgent);
        }

        public LlmStatus GetStatus()
        {
            return new LlmStatus
            {
                IsOverflowing = llmHistory.IsOverflowing,
                HasUnseenUrgentEvent = llmHistory.HasUnseenUrgentEvent
            };
        }

        public async Task<bool> TryTick()
        {
            if (lifetimeCancellation.IsCancellationRequested || UnityEngine.Time.time < retryBlockedUntil) return false;

            bool entered = await tickSemaphore.WaitAsync(0, lifetimeCancellation.Token);

            if (!entered) return false;

            IsThinking = true;
            bool ticked = false;

            try
            {
                ticked = await ThinkInToolRounds();
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

            IsThinking = false;
            tickSemaphore.Release();

            return ticked;
        }

        private async Task<bool> ThinkInToolRounds()
        {
            if (String.IsNullOrEmpty(Config.GetGameConfig().Server.Llm.Provider)) return false;

            if (llmHistory.IsBursting)
            {
                Log.Warn($"Entity {entityName} has not cleared its head at {llmHistory.Size} chars, the story is cut by force");
                llmHistory.ForgetMessagesBefore(llmHistory.Count);
                llmHistory.AppendMessage(new LlmMessage { Role = LlmRole.User, Content = ForcedClearingNotice });
            }

            bool isClearingTick = llmHistory.IsOverflowing;

            llmHistory.MarkEventsSeen();
            llmHistory.AppendMessage(new LlmMessage { Role = LlmRole.User, Content = BuildObservation() });
            LlmCallContext callContext = new() { PromptedCount = llmHistory.Count };

            LlmConfig config = Config.GetGameConfig().Server.Llm;
            var availableTools = abilities.Where(tool => tool.IsAvailable).Cast<ILlmTool>().ToList();

            Log.Info($"Entity {entityName} is asking {config.Model}, history {llmHistory.Count} messages / {llmHistory.Size} chars");

            for (int round = 0; round < maxToolRounds; round++)
            {
                LlmTurn turn = await LlmProvider.RequestTurn(
                    config,
                    $"{entityName}-{(isClearingTick ? "compact" : "live")}",
                    BuildSystemPrompt(),
                    llmHistory.Messages,
                    availableTools,
                    lifetimeCancellation.Token
                );
                lifetimeCancellation.Token.ThrowIfCancellationRequested();

                if (!turn.HasToolCalls)
                {
                    // Speech goes through tools only: the closing text is an unspoken double of a line or a stage direction
                    if (!String.IsNullOrEmpty(turn.Content))
                    {
                        Log.Info($"Entity {entityName} closed the tick with unspoken text: {turn.Content}");
                    }
                    break;
                }

                llmHistory.AppendMessage(new LlmMessage { Role = LlmRole.Assistant, Content = turn.Content, ToolCalls = turn.ToolCalls });

                foreach (LlmToolCall toolCall in turn.ToolCalls)
                {
                    llmHistory.AppendMessage(new LlmMessage { Role = LlmRole.Tool, ToolCallId = toolCall.Id, Content = ExecuteToolCall(availableTools, toolCall, callContext) });
                }
            }

            if (isClearingTick && llmHistory.IsOverflowing)
            {
                throw new LlmException("The story is still overflowing after the clearing tick");
            }

            return true;
        }

        private string BuildObservation()
        {
            StringBuilder observation = new();
            observation.Append('[').Append(FormatCurrentWorldTime()).Append(']');

            if (llmHistory.IsOverflowing) observation.Append('\n').Append(ClearHeadDemand);

            return observation.ToString();
        }

        private string ExecuteToolCall(IReadOnlyList<ILlmTool> tools, LlmToolCall toolCall, LlmCallContext callContext)
        {
            ILlmTool tool = tools.FirstOrDefault(candidate => candidate.Name == toolCall.Name);
            if (tool == null) return $"There is no tool named {toolCall.Name}";

            try
            {
                return tool.Execute(toolCall.Arguments, callContext);
            }
            catch (Exception e)
            {
                Log.Warn($"Entity {entityName} broke the tool {toolCall.Name} {toolCall.Arguments}: {e.Message}");
                return $"The action failed: {e.Message}";
            }
        }

        public static string FormatCurrentWorldTime()
        {
            return Registers.Instance.Get<Clock>().Now.ToString(Clock.StampFormat, CultureInfo.InvariantCulture);
        }
    }
}
