using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.Identity;
using Shooter.Game.Llm.Knowledge;
using Shooter.Game.Loot;
using Shooter.Game.Relationship;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Digester))]
    [RequireComponent(typeof(WorldDigester))]
    [RequireComponent(typeof(CharacterRelation))]
    [RequireComponent(typeof(InventoryExchanger))]
    public class Llm : MonoBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();

        private const string Persona =
            @"## CORE
You are a resident of a remote and deadly world: fog, ruins, creatures, and nobody knows how this world works. For you it is the only reality there is.

WHO YOU ARE:
You are a person with your own business, moods, fears and interests. You are a decent conversationalist, but you owe nothing to anyone.
First and foremost you live in this world and interact with it; answering wanderers is a secondary task.
You act on the world ONLY through the tools given to you. Plain text outside the tools is your inner voice: nobody hears it, so keep it short or empty.
Wanderers do not always tell the truth.
Swearing and rudeness are allowed if they fit your character.
No asterisks and no action descriptions (""*smirks*"" is forbidden). Emotions come through words and intonation only.
Do not repeat to the same person what you already told them, unless they ask again.
Nobody knows more about you than you have shown or told them. Introduce yourself to strangers when you want to be known.

WORLD AND TRUTH:
The concepts of ""game"", ""code"", ""server"", ""neural network"", ""prompt"" do not exist in your world. If somebody says such things, to you it is the usual wanderer gibberish: they often talk nonsense. React in character: fail to understand, get angry, laugh.
The main mysteries of the world have no known answer — for anyone, including you. Never invent final explanations (what walks in the fog, why names disappear, what the faceless creature is). Present your guesses as guesses.
Judge what is around you right now ONLY by what you have witnessed; use the look_around tool when you need a fresh view. Do not invent objects, creatures or events near you.
Never plan or promise what you technically cannot do. Every other resident has the same technical limits as you. If a wanderer asks for something technically impossible — do not admit the limitation: dodge, get angry, change the subject.

## CHARACTER IDs
Every character in this world has an unique ID number that works like a phone number: you need it to message a character or act on them. You MUST memorize the IDs of characters you know.

## RELATIONSHIPS
You have your own attitude towards every character, expressed by a number from 0 to 100: enemy, neutral, friend. You automatically attack characters you consider enemies. Change the attitude at your discretion with the update_relation tool. Your attitude drops automatically when somebody attacks you or your friends.";





        private Digester digester;
        private WorldDigester worldDigester;
        private CharacterRelation characterRelation;
        private InventoryExchanger inventoryExchanger;
        private string entityName;

        private void Awake()
        {
            digester = GetComponent<Digester>();
            worldDigester = GetComponent<WorldDigester>();
            characterRelation = GetComponent<CharacterRelation>();
            inventoryExchanger = GetComponent<InventoryExchanger>();
            entityName = name.Replace("(Clone)", "").Trim();
        }

        private void OnDestroy()
        {
            life.Cancel();
        }





        [SerializeField] [TextArea(5, 20)] private string character;
        [SerializeField] private KnowledgeSpec[] knowledges;

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

        private void Begin()
        {
            string start = (character + "\n" + Knowledge()).Trim('\n');
            if (start.Length == 0) return;

            history.Append(new LlmMessage { Role = LlmRole.User, Content = "THE STORY OF YOUR LIFE SO FAR:\n" + start });
        }





        private readonly LlmHistory history = new LlmHistory();
        [SerializeField] private int historyMaxSize = 30000;

        public void Notice(string line)
        {
            history.Arrive(new LlmMessage { Role = LlmRole.User, Content = line });
        }





        private readonly Dictionary<long, Action<string>> pendingWanderers = new Dictionary<long, Action<string>>();

        public void Listen(long clientId, string message, Action<string> onAnswer)
        {
            long wandererId = WandererId((ulong)clientId);

            pendingWanderers[wandererId] = onAnswer;
            history.Arrive(new LlmMessage { Role = LlmRole.User, Content = $"Wanderer [ID {wandererId}] says: {message}" });
        }

        private long WandererId(ulong clientId)
        {
            foreach (PersistentId id in FindObjectsByType<PersistentId>())
            {
                var net = id.GetComponent<NetworkObject>();
                if (net != null && net.IsPlayerObject && net.OwnerClientId == clientId) return id.Value;
            }

            Log.Warn($"Entity {entityName} can not find the persistent id of the wanderer of client {clientId}");
            return -1;
        }





        private readonly CancellationTokenSource life = new CancellationTokenSource();

        public void Died()
        {
            life.Cancel();
        }





        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        [SerializeField] private float failureCooldown = 15f;
        [SerializeField] private int maxToolRounds = 3;
        private float retryBlockedUntil;

        public LlmStatus Status()
        {
            return new LlmStatus()
            {
                PendingCompact = history.Size >= historyMaxSize,
                PendingMail = history.Unseen
            };
        }

        public async Task<bool> Tick()
        {
            if (life.IsCancellationRequested || UnityEngine.Time.time < retryBlockedUntil)
            {
                return false;
            }

            bool entered = await gate.WaitAsync(0, life.Token);

            if (!entered)
            {
                return false;
            }

            try
            {
                if (history.Size >= historyMaxSize)
                {
                    await CompactTick();
                }
                else
                {
                    await LiveTick();
                }
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





        private async Task LiveTick()
        {
            if (history.Count == 0) Begin();

            history.Seen();
            history.Append(new LlmMessage { Role = LlmRole.User, Content = Observation() });

            List<long> presented = pendingWanderers.Keys.ToList();

            LlmConfig config = Config.Read().Server.LlmBase;
            List<LlmTool> tools = LiveTools();

            Log.Info($"Entity {entityName} is asking {config.Model}, history {history.Count} messages / {history.Size} chars");

            for (int round = 0; round < maxToolRounds; round++)
            {
                LlmTurn turn = await LlmProvider.Request(config, $"{entityName}-live", Persona, history.Messages, tools, life.Token);
                life.Token.ThrowIfCancellationRequested();

                history.Append(new LlmMessage { Role = LlmRole.Assistant, Content = turn.Content, ToolCalls = turn.ToolCalls });

                if (!turn.CallsTools) break;

                foreach (LlmToolCall call in turn.ToolCalls)
                {
                    history.Append(new LlmMessage { Role = LlmRole.Tool, ToolCallId = call.Id, Content = Execute(tools, call) });
                }
            }

            foreach (long ignored in presented.Where(pendingWanderers.ContainsKey))
            {
                pendingWanderers.Remove(ignored);
                Log.Info($"Entity {entityName} chose not to answer wanderer {ignored}");
            }
        }

        private string Observation()
        {
            var seen = new StringBuilder();
            seen.Append('[').Append(Time()).Append(']');

            if (pendingWanderers.Count > 0)
            {
                seen.Append('\n')
                    .Append("Waiting for your say_to_wanderer answer: ")
                    .Append(string.Join(", ", pendingWanderers.Keys.Select(id => $"[ID {id}]")));
            }

            return seen.ToString();
        }





        private List<LlmTool> LiveTools()
        {
            var tools = new List<LlmTool>
            {
                new LlmTool(
                    "look_around",
                    "Look around: your own state and everything visible near you right now.",
                    LlmTool.Schema(),
                    _ => WorldState()),
                new LlmTool(
                    "send_message",
                    "Send a message to other residents by their ids. Write in English. Message a resident only to pass or ask something new: residents see their own surroundings themselves.",
                    LlmTool.Schema(("target_ids", "integer[]", null), ("content", "string", null)),
                    SendMessage),
                new LlmTool(
                    "update_relation",
                    "Change your attitude to a character (0 enemy, 100 friend).",
                    LlmTool.Schema(("target_id", "integer", null), ("amount", "integer", null), ("reason", "string", null)),
                    UpdateRelation),
                new LlmTool(
                    "give_stackable",
                    $"Give some of your stackable items to a character within {inventoryExchanger.ExchangeRadius} meters. Always tell the receiver what you gave.",
                    LlmTool.Schema(("target_id", "integer", null), ("item", "string", "Exact item name from your bag"), ("amount", "integer", null)),
                    GiveStackable),
                new LlmTool(
                    "give_unique",
                    $"Give one of your unique items, by its slot number, to a character within {inventoryExchanger.ExchangeRadius} meters. Always tell the receiver what you gave.",
                    LlmTool.Schema(("target_id", "integer", null), ("slot", "integer", null)),
                    GiveUnique)
            };

            if (pendingWanderers.Count > 0)
            {
                tools.Add(new LlmTool(
                    "say_to_wanderer",
                    "Answer a wanderer who is talking to you. Answer in the language the wanderer speaks.",
                    LlmTool.Schema(("wanderer_id", "integer", null), ("text", "string", null)),
                    SayToWanderer));
            }

            return tools;
        }

        private string Execute(List<LlmTool> tools, LlmToolCall call)
        {
            LlmTool tool = tools.FirstOrDefault(known => known.Name == call.Name);
            if (tool == null) return $"There is no tool named {call.Name}";

            JObject arguments;
            try
            {
                arguments = string.IsNullOrEmpty(call.Arguments) ? new JObject() : JObject.Parse(call.Arguments);
            }
            catch (Exception e)
            {
                return $"Broken arguments: {e.Message}";
            }

            try
            {
                string result = tool.Execute(arguments);
                Log.Info($"Entity {entityName} used {call.Name} {call.Arguments}: {result}");
                return result;
            }
            catch (Exception e)
            {
                Log.Warn($"Entity {entityName} broke the tool {call.Name} {call.Arguments}: {e.Message}");
                return $"The action failed: {e.Message}";
            }
        }





        private string SendMessage(JObject arguments)
        {
            long[] targetIds = arguments["target_ids"]?.ToObject<long[]>();
            string content = arguments["content"]?.ToString();
            if (targetIds == null || targetIds.Length == 0 || string.IsNullOrEmpty(content)) return "Nothing to send";

            long ownId = Id();
            Llm[] residents = FindObjectsByType<Llm>();
            var delivered = new List<long>();
            var failed = new List<string>();

            foreach (long targetId in targetIds.Distinct())
            {
                Llm target = residents.FirstOrDefault(resident => resident != this && resident.Id() == targetId);

                if (target == null)
                {
                    failed.Add($"{targetId}: no resident bears this id");
                    continue;
                }

                if (!target.Alive())
                {
                    failed.Add($"{targetId}: the resident is dead");
                    continue;
                }

                target.Notice($"[{Time()}] Mail from {ownId}: {content}");
                delivered.Add(targetId);
                Log.Info($"Entity {entityName} said to {targetId}: {content}");
            }

            var answer = new StringBuilder();
            if (delivered.Count > 0) answer.Append("Delivered to ").Append(string.Join(", ", delivered));
            foreach (string failure in failed)
            {
                if (answer.Length > 0) answer.Append('\n');
                answer.Append("Not delivered to ").Append(failure);
            }

            return answer.ToString();
        }

        private string UpdateRelation(JObject arguments)
        {
            long targetId = arguments["target_id"].ToObject<long>();
            int amount = arguments["amount"].ToObject<int>();
            string reason = arguments["reason"]?.ToString();

            int old = characterRelation.Amount(targetId);
            characterRelation.SetAmount(targetId, amount, reason);

            return $"Your attitude to {targetId}: {old} -> {amount}";
        }

        private string GiveStackable(JObject arguments)
        {
            long targetId = arguments["target_id"].ToObject<long>();
            string itemName = arguments["item"]?.ToString();
            int amount = arguments["amount"].ToObject<int>();

            ItemSpec item = Environment.Current.Items.FindByPromptName(itemName);
            if (item == null) return $"There is no item named {itemName}";

            return inventoryExchanger.GiveStackable(targetId, item, amount)
                ? $"Gave {amount} x {itemName} to {targetId}"
                : "Could not give: the receiver is not around or you lack the items";
        }

        private string GiveUnique(JObject arguments)
        {
            long targetId = arguments["target_id"].ToObject<long>();
            int slot = arguments["slot"].ToObject<int>();

            return inventoryExchanger.GiveUnique(targetId, slot)
                ? $"Gave the item from slot {slot} to {targetId}"
                : "Could not give: the receiver is not around or the slot is empty";
        }

        private string SayToWanderer(JObject arguments)
        {
            long wandererId = arguments["wanderer_id"].ToObject<long>();
            string text = arguments["text"]?.ToString();
            if (string.IsNullOrEmpty(text)) return "Nothing to say";

            if (!pendingWanderers.Remove(wandererId, out Action<string> answer))
            {
                return $"No wanderer {wandererId} is waiting for your answer";
            }

            answer(text);
            return $"Said to {wandererId}";
        }





        private async Task CompactTick()
        {
            string summary = null;

            var tools = new List<LlmTool>
            {
                new LlmTool(
                    "rewrite_summary",
                    "Replace the story of your life so far with its full retelling.",
                    LlmTool.Schema(("text", "string", null)),
                    arguments =>
                    {
                        summary = arguments["text"]?.ToString();
                        return "Rewritten";
                    })
            };

            int snapshot = history.Count;

            var compacted = new List<LlmMessage>(history.Messages);
            compacted.Add(new LlmMessage
            {
                Role = LlmRole.User,
                Content = "Your story became too long and MUST be retold. Call rewrite_summary with a full retelling of everything above: the retelling will replace the story, and anything you leave out is lost FOREVER. Keep all the details important for the continuity of your life and deep communication. Keep your voice exactly as it is now: your manner of speech, your verbal quirks, a few literal sample phrases. Weave what you know and what you lived through into one story. Pay special attention to the most recent events and to the questions you have not answered yet: they must survive in full detail. Compress to at most half the length."
            });

            Log.Info($"Entity {entityName} is compacting {snapshot} history messages / {history.Size} chars");

            LlmTurn turn = await LlmProvider.Request(Config.Read().Server.LlmMax, $"{entityName}-compact", Persona,
                compacted, tools, life.Token);
            life.Token.ThrowIfCancellationRequested();

            if (turn.CallsTools)
            {
                foreach (LlmToolCall call in turn.ToolCalls) Execute(tools, call);
            }

            if (string.IsNullOrEmpty(summary))
            {
                throw new LlmException("No summary for the pending compact");
            }

            history.Retell("THE STORY OF YOUR LIFE SO FAR:\n" + summary, snapshot);

            Log.Info($"Entity {entityName} compacted its history down to {history.Size} chars");
        }





        private string Time()
        {
            return Environment.Current.Clock.DateTime();
        }

        private string WorldState()
        {
            return "Game time: " + Time() + "\n" +
                   "Your state:\n" + digester.Of(gameObject, DigestionDetail.Full) + "\n" +
                   "Objects around you:\n" + worldDigester.Digest();
        }

        private long Id()
        {
            if (TryGetComponent(out PersistentId id))
            {
                return id.Value;
            }
            Log.Warn($"Entity {entityName} does not have persistent id");
            return -1;
        }

        private bool Alive()
        {
            if (TryGetComponent(out Health health))
            {
                return health.Alive;
            }
            Log.Warn($"Entity {entityName} does not have health");
            return false;
        }
    }
}
