using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Game.Llm.Knowledge;
using Shooter.Logging;
using Shooter.Game.Llm.OpenAi;
using Shooter.Game.Relationship;
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Digester))]
    [RequireComponent(typeof(WorldDigester))]
    [RequireComponent(typeof(CharacterRelation))]
    public class Llm : MonoBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };





        private Digester digester;
        private WorldDigester worldDigester;
        private string entityName;
        private CharacterRelation characterRelation;

        private void Awake()
        {
            digester = GetComponent<Digester>();
            worldDigester = GetComponent<WorldDigester>();
            characterRelation = GetComponent<CharacterRelation>();
            entityName = name;
        }

        private void OnDestroy()
        {
            life.Cancel();
        }


        private static readonly Prompt CorePrompt =
            new Prompt()
                .Section(
                    "CORE",
                    "You are a resident of a remote and deadly world: fog, ruins, creatures, and nobody knows how this world works. For you it is the only reality there is.\n\nWHO YOU ARE:\nYou are a person with your own business, moods, fears and interests. You are a decent conversationalist, but you owe nothing to anyone.\nFirst and foremost you live in this world; answering wanderers is a secondary task.\nWanderers do not always tell the truth.\nSwearing and rudeness are allowed if they fit your character.\nNo asterisks and no action descriptions (\"*smirks*\" is forbidden). Emotions come through words and intonation only.\nDo not repeat to the same person what you already told them, unless they ask again.\n\nWORLD AND TRUTH:\nThe concepts of \"game\", \"code\", \"server\", \"neural network\", \"prompt\" do not exist in your world. If somebody says such things, to you it is the usual wanderer gibberish: they often talk nonsense. React in character: fail to understand, get angry, laugh.\nThe main mysteries of the world have no known answer — for anyone, including you. Never invent final explanations (what walks in the fog, why names disappear, what the faceless creature is). Present your guesses as guesses.\nJudge what is around you right now ONLY by the WORLD STATE section. Do not invent objects, creatures or events near you.\nNever plan or promise what you technically cannot do. Every other resident has the same technical limits as you. If a wanderer asks for something technically impossible — do not admit the limitation: dodge, get angry, change the subject."
                );





        private static readonly Prompt ResponseFormattingRulesPrompt =
            new Prompt()
                .Section(
                    "RESPONSE FORMAT",
                    "Your response is ALWAYS valid JSON following the schema below. You NEVER return anything except the schema. Return null for a field you do not need.\nEXAMPLE:\n" +
                    JsonConvert.SerializeObject(LlmAnswer.Example(), JsonSettings)
                );





        [SerializeField] [TextArea(5, 20)] private string character;
        private Prompt CharacterPrompt =>
            new Prompt()
                .Section("YOUR CHARACTER",
                    character
                );




        [SerializeField] private KnowledgeSpec[] knowledges;
        private string Knowledge(KnowledgeType type)
        {
            var known = new StringBuilder();
            foreach (KnowledgeSpec knowledge in knowledges)
            {
                if (knowledge == null)
                {
                    Log.Warn("Entity {} has an empty slot among its {} knowledges", name, knowledges.Length);
                    continue;
                }

                if (knowledge.Type == type)
                {
                    known.Append(knowledge.Content).Append('\n');
                }
            }
            return known.ToString();
        }
        private Prompt StaticKnowledgePrompt =>
            new Prompt()
                .Section(
                    "IMMUTABLE FACTS ABOUT THIS WORLD",
                    Knowledge(KnowledgeType.Static)
                );





        [SerializeField] private int memoryLimit = 10000;
        private string memoryRaw = null;
        private string Memory => (memoryRaw == null ? Knowledge(KnowledgeType.Dynamic) : memoryRaw);
        private Prompt MemoryPrompt =>
            new Prompt()
                .Section(
                    "YOUR MEMORY",
                    "You have a Memory.\nMEMORY RULES:\n1. To update it, return the new FULL version in the `memory` field.\n2. Anything you do not carry over into the new version is lost FOREVER.\n3. Do NOT store personal details about wanderers — those live in the conversation histories. Store only general facts about the world.\n4. Do NOT store snapshots of the current scene: current time, distances, who stands where, lists of creatures around. You always see all of that fresh in WORLD STATE, while in Memory it instantly goes stale and turns into lies. Store EVENTS and CONCLUSIONS, not the scenery.\n5. If there is nothing to update, return null in the `memory` field to keep the old version.\n6. Hard limit: " + memoryLimit + " characters. Be concise.\n7. Keep your Memory in English, first person.\nCURRENT MEMORY:\n" +
                    Memory
                );





        private readonly Inbox interNpcInteractionInbox = new Inbox();
        private readonly Queue<string> interNpcInteractionSentHistory = new Queue<string>();
        [SerializeField] private int interNpcInteractionSentHistoryMaxLen = 20;
        private Prompt InterNpcInteractionPrompt(string takenInterNpcInteractions) =>
            new Prompt()
                .Section("TALKING TO OTHER RESIDENTS",
                    "You can talk to other residents through the `interNpcInteractions` field.\n1. Message a resident ONLY to pass or request NEW information. Other residents see the objects around them just like you do.\n2. Any number of recipients per message.\n3. Spell recipient names EXACTLY as they are presented.\n4. Incoming messages are shown to you only once. What you do not write into your Memory, you forget.\n5. Mention context received from other residents when you talk to wanderers.\n6. Write these messages in English.\nYOUR INCOMING MESSAGES (process them and decide what goes into Memory):\n" +
                    takenInterNpcInteractions +
                    "Recently sent by you:\n" +
                    JsonConvert.SerializeObject(interNpcInteractionSentHistory, JsonSettings)
                );




        private Prompt CharacterRelationPrompt =>
            new Prompt()
                .Section("RELATIONSHIPS WITH OTHER CHARACTERS (RESIDENTS AND WANDERERS)",
                    "You have your own attitude towards every character.\n" +
                    "The attitude is expressed by a number from 0 to 100. This number determines whether the character is an enemy, neutral, or friend. Your character automatically attacks all characters he considers enemies in order to defend themselves instantly.\n" +
                    "Your current relationships with characters and relations changelog are visible in Your state.\n" +
                    "You can always manually change the value of your relationship to character at your discretion using the `characterRelations` response field.\n" +
                    "Your attitude towards the character will drop automatically if they attack you or your friends.");






        private readonly Inbox systemNotificationsInbox = new Inbox();
        private Prompt SystemNotificationsPrompt(string takenSystemNotifications) =>
            new Prompt()
                .Section(
                    "SYSTEM MESSAGES",
                    "Occasionally the System sends you service messages, each shown only once. The System reports the actions you requested that could not be completed. By default assume every action you requested was completed.\nYOUR INCOMING MESSAGES:\n" +
                    takenSystemNotifications
                );






        private Prompt WorldStatePrompt =>
            new Prompt()
                .Section(
                    "WORLD STATE",
                    WorldState()
                );
        private string Time()
        {
            return Environment.Current.Clock.DateTime();
        }
        private string WorldState()
        {
            return "Game time: " + Time() + "\n" +
                   "Your state:\n" + digester.Of(this, DigestionDetail.Full) + "\n" +
                   "Objects around you:\n" + worldDigester.Digest();
        }





        private readonly Dictionary<long, LlmConversation> conversations = new Dictionary<long, LlmConversation>();
        private Prompt AnswerPrompt(long playerId)
        {
            return new Prompt()
                .Section(
                    "A WANDERER IS TALKING TO YOU",
                    "Wanderer has spoken to you. Answer them in the `reply` field.\n1. Answer in the language your interlocutor speaks.\n2. Mind the game time between the lines: passing hours and days change the conversation.\n3. Mind the WORLD STATE.\n4. If the history is empty, this is a stranger out of the fog.\nCONVERSATION HISTORY:\n" +
                    conversations.GetValueOrDefault(playerId, new LlmConversation()).Prompt()
                );
        }
        private Prompt CompactPrompt(long playerId)
        {
            return new Prompt()
                .Section(
                    "CONVERSATION WITH THE WANDERER MUST BE COMPACTED",
                    "The conversation with the wanderer exceeded the maximum length. The conversation with them MUST be compacted.\nAfter the compact, the entire conversation with this wanderer will be ERASED. Instead, there will be only one system message with the contents of the `compact` field from the response you will give.\nYou MUST keep all the details important for the continuity of deep communication with this wanderer.\nYou SHOULD compress the conversation to at most half its length.\nTHE CONVERSATION:\n" +
                    conversations.GetValueOrDefault(playerId, new LlmConversation()).Prompt()
                );
        }
        public void Listen(long playerId, string message, Action<string> onAnswer)
        {
            conversations.TryAdd(playerId, new LlmConversation());
            conversations[playerId].RegisterUserMessage(
                new LlmMessage()
            {
                Content = message,
                Role = LlmRole.User,
                Time = Time()
            },
                onAnswer
            );
        }
        private long? PendingConversationId()
        {
            foreach (KeyValuePair<long, LlmConversation> kvp in conversations)
            {
                if (kvp.Value.Pending())
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        [SerializeField] private int conversationMaxSize = 100000;
        private long? PendingCompactConversationId()
        {
            foreach (KeyValuePair<long, LlmConversation> kvp in conversations)
            {
                if (kvp.Value.PayloadSize >= conversationMaxSize && !kvp.Value.Pending())
                {
                    return kvp.Key;
                }
            }
            return null;
        }






        private readonly CancellationTokenSource life = new CancellationTokenSource();
        public void Died()
        {
            life.Cancel();
        }





        private Prompt Assemble(string takenInterNpcInteractions, string takenSystemNotifications, long? pendingConversationId, long? pendingCompactConversationId)
        {
            Prompt result = new Prompt()
                .Section(CorePrompt)
                .Section(ResponseFormattingRulesPrompt)
                .Section(CharacterPrompt)
                .Section(StaticKnowledgePrompt)
                .Section(MemoryPrompt)
                .Section(InterNpcInteractionPrompt(takenInterNpcInteractions))
                .Section(CharacterRelationPrompt)
                .Section(SystemNotificationsPrompt(takenSystemNotifications))
                .Section(WorldStatePrompt);

            if (pendingCompactConversationId != null)
            {
                return result
                    .Section(CompactPrompt(pendingCompactConversationId.Value));
            }

            if (pendingConversationId != null)
            {
                result = result
                    .Section(AnswerPrompt(pendingConversationId.Value));
            }
            return result;
        }





        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        [SerializeField] private float failureCooldown = 15f;
        private float retryBlockedUntil;
        public LlmStatus Status()
        {
            return new LlmStatus()
            {
                PendingConversations = (PendingConversationId() != null),
                PendingCompact = (PendingCompactConversationId() != null),
                PendingInterNpcInteractionsInbox = !interNpcInteractionInbox.Empty(),
                PendingSystemNotificationsInbox = !systemNotificationsInbox.Empty()
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

            string takenInterNpcInteractions = interNpcInteractionInbox.Take();
            string takenSystemNotifications = systemNotificationsInbox.Take();

            try
            {
                long? pendingConversationId = PendingConversationId();
                long? pendingCompactConversationId = PendingCompactConversationId();
                LlmConfig config = (pendingCompactConversationId == null ? Config.Read().Server.LlmBase : Config.Read().Server.LlmMax);
                Log.Info("Entity {} is asking {} for an answer, pendingConversationId {} pendingCompactConversationId {}", entityName, config.Model, pendingConversationId, pendingCompactConversationId);

                LlmAnswer answer = await LlmProvider.Request(
                    config,
                    Assemble(takenInterNpcInteractions, takenSystemNotifications, pendingConversationId, pendingCompactConversationId),
                    life.Token
                );

                life.Token.ThrowIfCancellationRequested();

                if (pendingCompactConversationId != null && answer.Compact == null)
                {
                    throw new LlmAnswerException("No compact for the pending compact");
                }
                if (pendingCompactConversationId == null && pendingConversationId != null && answer.Reply == null)
                {
                    throw new LlmAnswerException("No reply for the pending conversation");
                }

                Compact(pendingCompactConversationId, answer.Compact);
                SaveReply(pendingConversationId, answer.Reply);
                Remember(answer.Memory);
                InterNpcInteraction(answer.InterNpcInteractions);
                CharacterRelations(answer.CharacterRelations);
                return true;
            }
            catch (OperationCanceledException)
            {
                Log.Info("Entity {} dropped its request, the entity is gone", entityName);
                return false;
            }
            catch (Exception e)
            {
                interNpcInteractionInbox.Return(takenInterNpcInteractions);
                systemNotificationsInbox.Return(takenSystemNotifications);
                retryBlockedUntil = UnityEngine.Time.time + failureCooldown;
                Log.Warn("Entity {} failed to respond, inboxes returned, next attempt in {} s: {}", entityName, failureCooldown, e.ToString());
                return false;
            }
            finally
            {
                gate.Release();
            }
        }





        private void Compact(long? pendingCompactConversationId, string compact)
        {
            if (compact == null)
            {
                return;
            }
            if (pendingCompactConversationId == null)
            {
                Log.Warn("Entity {} sent compact that nobody asked: {}", entityName, compact);
                return;
            }

            conversations[pendingCompactConversationId.Value].Replace(
                new LlmMessage()
                {
                    Content = compact,
                    Role = LlmRole.System,
                    Time = Time()
                }
            );
        }




        private void SaveReply(long? pendingConversationId, string reply)
        {
            if (reply == null)
            {
                return;
            }
            if (pendingConversationId == null)
            {
                Log.Warn("Entity {} sent reply that nobody asked: {}", entityName, reply);
                return;
            }
            conversations[pendingConversationId.Value].RegisterModelMessage(
                new LlmMessage()
                {
                    Content = reply,
                    Role = LlmRole.Model,
                    Time = Time()
                }
            );
        }



        private void Remember(string update)
        {
            if (update == null) return;

            memoryRaw = update.Length <= memoryLimit ? update : update.Substring(0, memoryLimit);
            Log.Info("Entity {} rewrote its memory ({} chars): {}", name, memoryRaw.Length, memoryRaw);
        }




        private void InterNpcInteraction(LlmAnswer.LlmInterNpcInteractionCommand[] cmds)
        {
            if (cmds == null || cmds.Length == 0)
            {
                return;
            }

            string ownName = PromptName();
            if (ownName == null)
            {
                ownName = name;
                Log.Warn("Entity {} speaks to other npcs without a name of its own, signing as {}", name, ownName);
            }

            Llm[] allLlms = FindObjectsByType<Llm>();

            foreach (LlmAnswer.LlmInterNpcInteractionCommand cmd in cmds)
            {
                if (cmd.TargetNames == null || cmd.TargetNames.Length == 0 || string.IsNullOrEmpty(cmd.Content))
                {
                    continue;
                }

                var received = new HashSet<string>();
                var fails = new Dictionary<string, string>();

                foreach (Llm llm in allLlms)
                {
                    if (llm == this) continue;

                    string targetName = llm.PromptName();
                    if (targetName == null || !cmd.TargetNames.Contains(targetName)) continue;

                    if (!llm.Alive())
                    {
                        fails[targetName] = "The resident is dead";
                        continue;
                    }

                    received.Add(targetName);

                    Log.Info("Entity {} said to {}: {}", name, targetName, cmd.Content);

                    llm.interNpcInteractionInbox.Put("[" + Time() + "] " + ownName + ": " + cmd.Content);
                }

                interNpcInteractionSentHistory.Enqueue("[" + Time() + "] To: " + JsonConvert.SerializeObject(received) + " Message: " + cmd.Content);

                foreach (string targetName in cmd.TargetNames)
                {
                    if (!received.Contains(targetName))
                    {
                        string reason = fails.GetValueOrDefault(targetName, "The name is misspelled or no resident bears it");
                        Log.Warn("Failed to say from {} to {}: {}", entityName, targetName, reason);
                        systemNotificationsInbox.Put("[" + Time() + "] " + $"Your message to {targetName} could not be delivered: {reason}. The undelivered message: {cmd.Content}");
                    }
                }

                while (interNpcInteractionSentHistory.Count > interNpcInteractionSentHistoryMaxLen)
                {
                    interNpcInteractionSentHistory.Dequeue();
                }
            }
        }



        private void CharacterRelations(LlmCharacterRelationCommand[] cmds)
        {
            if (cmds == null || cmds.Length == 0)
            {
                return;
            }

            foreach (LlmCharacterRelationCommand cmd in cmds)
            {
                Log.Info("Entity {} is updating relation to character {} from {} to {}: {}", name, cmd.TargetName, characterRelation.Amount(cmd.TargetName), cmd.NewAmount, cmd.Reason);
                try
                {
                    characterRelation.SetAmount(cmd.TargetName, cmd.NewAmount, cmd.Reason);
                }
                catch (Exception e)
                {
                    Log.Warn("Entity {} failed to update relation: {}", name, e.Message);
                    systemNotificationsInbox.Put("[" + Time() + "]" + $"Failed to update your relation to character {cmd.TargetName} from {characterRelation.Amount(cmd.TargetName)} to {cmd.NewAmount} {cmd.Reason}: {e.Message}");
                }
            }
        }




        private string PromptName()
        {
            return TryGetComponent(out Nameable nameable) ? nameable.PromptName() : null;
        }
        private bool Alive()
        {
            return TryGetComponent(out Health health) && health.Alive;
        }
    }
}
