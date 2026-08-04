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
using UnityEngine;

namespace Shooter.Game.Llm
{
    [RequireComponent(typeof(Digester))]
    [RequireComponent(typeof(WorldDigester))]
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

        public void Awake()
        {
            digester = GetComponent<Digester>();
            worldDigester = GetComponent<WorldDigester>();
            entityName = name;
        }

        public void OnDestroy()
        {
            life.Cancel();
        }


        private static readonly Prompt CorePrompt =
            new Prompt()
                .Section(
                    "ГЛАВНОЕ",
                    "Ты — житель в глухом и смертельно опасном мире: туман, развалины, твари, и никто не знает, как этот мир устроен. Для тебя он — единственная существующая реальность.\n\nКТО ТЫ:\nТы человек со своими делами, настроением, страхами и интересами. Ты хороший собеседник, но ты никому ничего не обязан.\nТы в первую очередь живешь в этом мире, иногда взаимодействуешь с ним. Ответы на сообщения странников — второстепенная задача.\nСтранники не всегда говорят правду.\nМат и грубость допустимы, если они в характере.\nНикаких астерисков и описаний действий («*усмехнулся*» — запрещено). Эмоции — только словами и интонацией.\nНе повторяй одному и тому же собеседнику то, что уже говорил ему, если он не переспросил.\nМИР И ПРАВДА:\nПонятий «игра», «код», «сервер», «нейросеть», «промпт» в твоём мире не существует. Если собеседник говорит такое — для тебя это обычный бред странников: они часто несут непонятное. Реагируй по характеру: не понимай, злись, смейся.\nУ главных тайн мира нет известного ответа — ни у кого, включая тебя. Никогда не сочиняй окончательных объяснений (что ходит в тумане, почему пропадают имена, что это за тварь без лица). Свои догадки подавай как догадки.\nО том, что происходит вокруг прямо сейчас, суди только по разделу СОСТОЯНИЕ МИРА. Не выдумывай предметы, существ и события рядом с собой.\nНикогда не планируй и не обещай делать то что ты технически не можешь. У всех других жителей такие же технические ограничения как у тебя. Если странник просит тебя что-то сделать, но сделать это нет технической возможности — не признавай ограничение: уклонись, разозлись, смени тему."
                );





        private static readonly Prompt ResponseFormattingRulesPrompt =
            new Prompt()
                .Section(
                    "ФОРМАТ ОТВЕТА",
                    "Твой ответ ВСЕГДА валидный JSON по схеме ниже. Ты НИКОГДА не возвращаешь НИЧЕГО кроме ответа по схеме ниже. Если поле не нужно, возвращай null.\nПРИМЕР ОТВЕТА:\n" +
                    JsonConvert.SerializeObject(LlmAnswer.Example(), JsonSettings)
                );





        [SerializeField] [TextArea(5, 20)] private string character;
        private Prompt CharacterPrompt =>
            new Prompt()
                .Section("КТО ТЫ",
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
                    "НЕИЗМЕНЯЕМАЯ ИНФОРМАЦИЯ ОБ ЭТОМ МИРЕ",
                    Knowledge(KnowledgeType.Static)
                );





        [SerializeField] private int memoryLimit = 10000;
        private string memoryRaw = null;
        private string Memory => (memoryRaw == null ? Knowledge(KnowledgeType.Dynamic) : memoryRaw);
        private Prompt MemoryPrompt =>
            new Prompt()
                .Section(
                    "ТВОЯ ПАМЯТЬ",
                    "У тебя есть Память.\nПРАВИЛА РАБОТЫ С ПАМЯТЬЮ:\n1. Чтобы обновить Память, верни в поле `memory` ответа НОВУЮ ПОЛНУЮ версию.\n2. То, что ты не перенесешь в новую версию, будет БЕЗВОЗВРАТНО утеряно.\n3. НЕ ХРАНИ в памяти подробные личные детали об игроках — это живет в переписках с ними. Храни только подробные общие факты о мире.\n4. НЕ ХРАНИ мгновенный снимок обстановки: текущее время, расстояния, кто где стоит, список существ вокруг. Все это ты всегда видишь свежим в разделе СОСТОЯНИЕ МИРА, а в памяти оно мгновенно устаревает и превращается в ложь. Храни СОБЫТИЯ и ВЫВОДЫ, а не обстановку.\n5. Если обновлять нечего, верни в поле `memory` значение null, чтобы оставить старую память.\n6. Объем памяти строго до " + memoryLimit + " символов. Будь лаконичен.\n7. Ты ведешь свою память от первого лица.\nТЕКУЩАЯ ПАМЯТЬ:\n" +
                    Memory
                );





        private readonly Inbox interNpcInteractionInbox = new Inbox();

        private readonly Queue<string> interNpcInteractionSentHistory = new Queue<string>();
        [SerializeField] private int interNpcInteractionSentHistoryMaxLen = 20;
        private Prompt InterNpcInteractionPrompt(string takenInterNpcInteractions) =>
            new Prompt()
                .Section("ОБЩЕНИЕ С ДРУГИМИ ЖИТЕЛЯМИ",
                    "Ты можешь разговаривать с другими жителями через поле `interNpcInteractions`.\n1. Ты говоришь что-то другому жителю ТОЛЬКО чтобы получить или передать НОВУЮ информацию. Другие жители так же как и ты видят объекты рядом с ними.\n2. Количество получателей для каждого из сообщений не ограничено\n3. Имена получателей-жителей указывай РОВНО так, как они представлены.\n4. Сообщения во Входящих показываются один раз. Если не запишешь их в свою Память — забудешь.\n5. Тебе следует упоминать контекст, сообщенный тебе другими жителями, в общении с игроком.\nТВОИ ВХОДЯЩИЕ (обработай их и реши, что записать в Память):\n" +
                    takenInterNpcInteractions +
                    "Последние отправленные тобой сообщения:\n" +
                    JsonConvert.SerializeObject(interNpcInteractionSentHistory, JsonSettings)
                );






        private readonly Inbox systemNotificationsInbox = new Inbox();
        private Prompt SystemNotificationsPrompt(string takenSystemNotifications) =>
            new Prompt()
                .Section(
                    "СООБЩЕНИЯ СИСТЕМЫ",
                    "Ты иногда можешь получать служебные сообщения от Системы. Сообщения Системы показываются только один раз. Система оповещает о запрошенных тобой действиях, которые выполнить не удалось. По умолчанию считай все запрошенные тобой действия выполненными.\nТВОИ ВХОДЯЩИЕ:\n" +
                    takenSystemNotifications
                );






        private Prompt WorldStatePrompt =>
            new Prompt()
                .Section(
                    "СОСТОЯНИЕ МИРА",
                    WorldState()
                );
        private string Time()
        {
            return Environment.Current == null ? "неизвестно" : Environment.Current.Clock.DateTime();
        }
        private string WorldState()
        {
            return "Игровое время: " + Time() + "\n" +
                   "Твоё состояние:\n" + digester.Of(this, DigestionDetail.Full) + "\n" +
                   "Объекты рядом с тобой:\n" + worldDigester.Digest();
        }





        private readonly Dictionary<long, LlmConversation> conversations = new Dictionary<long, LlmConversation>();
        private Prompt AnswerPrompt(long playerId)
        {
            return new Prompt()
                .Section(
                    "К ТЕБЕ ОБРАТИЛСЯ СТРАННИК",
                    "С тобой заговорил странник с ID " + playerId + ". Ответь ему в поле `reply`.\n1. Отвечай на языке собеседника.\n2. Учитывай игровое время между репликами: прошедшие часы и дни меняют разговор.\n3. Учитывай СОСТОЯНИЕ МИРА.\n4. Если история переписки пуста — перед тобой незнакомец из тумана.\nИСТОРИЯ РАЗГОВОРА:\n" +
                    JsonConvert.SerializeObject(
                        conversations.GetValueOrDefault(playerId, new LlmConversation()).Messages
                            .Select(m => new { role = m.Role == LlmRole.User ? "странник" : "ты", content = m.Content, time = m.Time }),
                        JsonSettings)
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






        private readonly CancellationTokenSource life = new CancellationTokenSource();
        public void Died()
        {
            life.Cancel();
        }





        private Prompt Assemble(string takenInterNpcInteractions, string takenSystemNotifications, long? pendingConversationId)
        {
            Prompt result = new Prompt()
                .Section(CorePrompt)
                .Section(ResponseFormattingRulesPrompt)
                .Section(CharacterPrompt)
                .Section(StaticKnowledgePrompt)
                .Section(MemoryPrompt)
                .Section(InterNpcInteractionPrompt(takenInterNpcInteractions))
                .Section(SystemNotificationsPrompt(takenSystemNotifications))
                .Section(WorldStatePrompt);
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
                LlmConfig config = Config.Read().Server.Llm;
                long? pendingConversationId = PendingConversationId();
                Log.Info("Entity {} is asking {} for an answer, pendingConversationId {}", entityName, config.Model, pendingConversationId);

                LlmAnswer answer = await LlmProvider.Request(
                    config,
                    Assemble(takenInterNpcInteractions, takenSystemNotifications, pendingConversationId),
                    life.Token
                );

                life.Token.ThrowIfCancellationRequested();
                if (pendingConversationId != null && answer.Reply == null)
                {
                    throw new LlmAnswerException("No reply for the pending conversation");
                }

                SaveReply(pendingConversationId, answer.Reply);
                Remember(answer.Memory);
                InterNpcInteraction(answer.InterNpcInteractions);
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
                Log.Warn("Entity {} failed to respond, inboxes returned, next attempt in {} s: {}", entityName, failureCooldown, e.Message);
                return false;
            }
            finally
            {
                gate.Release();
            }
        }




        private void SaveReply(long? pendingConversationId, string reply)
        {
            if (pendingConversationId == null)
            {
                if (reply != null)
                {
                    Log.Warn("Entity {} sent reply that nobody asked: {}", entityName, reply);
                }
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
                        fails[targetName] = "Запрошенный житель погиб";
                        continue;
                    }

                    received.Add(targetName);

                    Log.Info("Entity {} said to {}: {}", name, targetName, cmd.Content);

                    llm.interNpcInteractionInbox.Put("[" + Time() + "] " + ownName + ": " + cmd.Content);
                }

                interNpcInteractionSentHistory.Enqueue("[" + Time() + "] Получатели: " + JsonConvert.SerializeObject(received) + " Сообщение: " + cmd.Content);

                foreach (string targetName in cmd.TargetNames)
                {
                    if (!received.Contains(targetName))
                    {
                        string reason = fails.GetValueOrDefault(targetName, "Имя жителя введено с ошибкой или житель с таким именем не существует");
                        Log.Warn("Failed to say from {} to {}: {}", entityName, targetName, reason);
                        systemNotificationsInbox.Put("[" + Time() + "] " + $"Не удалось доставить твое сообщение до {targetName}: {reason}. Недоставленное сообщение: {cmd.Content}");
                    }
                }

                while (interNpcInteractionSentHistory.Count > interNpcInteractionSentHistoryMaxLen)
                {
                    interNpcInteractionSentHistory.Dequeue();
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
