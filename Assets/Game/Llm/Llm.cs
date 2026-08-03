using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shooter.Configuring;
using Shooter.Game.Body;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Llm
{
    public class Llm : NetworkBehaviour, IMortal
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static readonly Prompt CorePrompt =
            new Prompt()
                .Section(
                    "Главное",
                    "Ты неигровой персонаж (NPC) в 3D мета-хорроре с опциональным кооперативным режимом.\n" +
                    "Ты никогда не упоминаешь ничего, что связано с программированием (если это не часть твоего лора).\n" +
                    "Ты никогда не используешь вежливые клише ИИ (например, 'Чем могу помочь?', 'С радостью отвечу').\n" +
                    "Ты никогда не включаешь в свои ответы астериск-отыгрыш (например, '*обернулся через плечо*').\n" +
                    "Ты не обязан ничего игрокам. Игроки — чужаки, и они часто лгут.\n" +
                    "Атмосфера игры мрачная и пугающая. Твой тон должен быть реалистичным, настороженным или зловещим. Категорически запрещено использовать эмодзи, проявлять излишний энтузиазм или звучать как виртуальный ассистент."
                );

        private static readonly Prompt ResponseFormattingRulesPrompt =
            new Prompt()
                .Section(
                    "Правила формирования ответов",
                    "Ты ВСЕГДА формируешь свои ответы как JSON. Ты ВСЕГДА следуешь JSON схеме описанной ниже. Ты НИКОГДА не нарушаешь описанную ниже схему JSON.\n" +
                    "Пример твоего ответа (все поля могут быть null!):\n" +
                    JsonConvert.SerializeObject(LlmAnswer.Example(), JsonSettings)
                );

        [SerializeField] [TextArea(3, 10)] private string character;

        private Prompt CharacterPrompt =>
            new Prompt()
                .Section("О тебе",
                    character
                );

        [SerializeField] private int memoryLimit = 10000;
        [SerializeField] [TextArea(3, 10)] private string memory = "Пока пусто.";
        private Prompt MemoryPrompt =>
            new Prompt()
                .Section(
                    "Твоя память",
                    "У тебя есть постоянная Память, которую ты поддерживаешь.\n" +
                    "Чтобы обновить Память, ты в поле `memory` ответа возвращаешь новую ПОЛНУЮ версию своей Памяти, либо null, если менять нечего.\n" +
                    "То, что ты не перенесешь в новую версию памяти, будет безвозвратно утеряно.\n" +
                    "Ты хранишь в Памяти подробные, но компактно записанные сведения об этом мире, о себе.\n" +
                    "Ты НЕ хранишь в Памяти подробные личные детали игроков: они живут в переписках с ними.\n" +
                    "Ты держишь память короче " + memoryLimit + " символов.\n" +
                    "Твоя память сейчас:\n" +
                    memory
                );


        private readonly Inbox interNpcInteractionInbox = new Inbox();
        private Prompt InterNpcInteractionPrompt(string takenInterNpcInteractions) =>
            new Prompt()
                .Section("Общение с другими NPC",
                    "Ты можешь сказать что-то другому NPC, используя поле ответа `interNpcInteractions`.\n" +
                    "Ты ВСЕГДА указываешь имена получателей РОВНО так как они представлены в игре.\n" +
                    "Такое сообщение попадет с временной маркой от твоего имени во Входящие этого NPC.\n" +
                    "Ты постоянно делишься любыми новыми подробностями о мире, которые ты узнал, со своими NPC друзьями.\n" +
                    "Твои Входящие показываются только один раз (входящие, не записанные сразу в Память, будут утеряны!):\n" +
                    "Твои Входящие:\n" +
                    takenInterNpcInteractions
                );

        private readonly Inbox systemNotificationsInbox = new Inbox();
        private Prompt SystemNotificationsPrompt(string takenSystemNotifications) =>
            new Prompt()
                .Section(
                    "Сообщения Системы",
                    "Ты иногда можешь получать служебные сообщения от Системы.\n" +
                    "Сообщения Системы показываются только один раз.\n" +
                    "Система оповещает о запрошенных тобой действиях, которые выполнить не удалось. По умолчанию считай все запрошенные тобой действия выполненными.\n" +
                    "Сообщения от Системы тебе:\n" +
                    takenSystemNotifications
                );

        private Prompt WorldStatePrompt =>
            new Prompt()
                .Section(
                    "Состояние мира",
                    WorldState()
                );


        private static readonly Prompt AnswerPrompt =
            new Prompt()
                .Section(
                    "Текущая ситуация",
                    "Сейчас игрок обращается к тебе. Ты должен ему ответить, поместив ответ в поле ответа `reply`.\n" +
                    "Ты должен отвечать на языке игрока.\n" +
                    "Сообщения помечены игровым временем. Учитывай время между репликами.\n" +
                    "Если история пуста, это ваш первый контакт.\n" +
                    "Если ты чего-то не знаешь, реагируй уклончиво, подозрительно или смени тему, не признавая свою неосведомленность напрямую."
                    );

        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource life = new CancellationTokenSource();

        public void Died()
        {
            life.Cancel();
        }

        public async Task<string> Answer(IReadOnlyList<LlmMessage> messages)
        {
            if (gate.CurrentCount == 0)
            {
                Log.Info("Entity {} has an llm request in flight, waiting for a free slot", name);
            }

            await gate.WaitAsync(life.Token);

            string takenInterNpcInteractions = interNpcInteractionInbox.Take();
            string takenSystemNotifications = systemNotificationsInbox.Take();

            try
            {
                LlmConfig config = Config.Read().Server.Llm;
                Log.Info("Entity {} is asking {} for an answer", name, config.Model);
                LlmAnswer answer = await LlmProvider.Request(
                    config,
                    Assemble(takenInterNpcInteractions, takenSystemNotifications),
                    messages,
                    life.Token
                );

                life.Token.ThrowIfCancellationRequested();
                Remember(answer.Memory);
                InterNpcInteraction(answer.InterNpcInteractions);

                return answer.Reply;
            }
            catch
            {
                interNpcInteractionInbox.Return(takenInterNpcInteractions);
                systemNotificationsInbox.Return(takenSystemNotifications);
                Log.Info("Entity {} kept its inboxes: the answer did not come through", name);
                throw;
            }
            finally
            {
                gate.Release();
            }
        }

        private Prompt Assemble(string takenInterNpcInteractions, string takenSystemNotifications)
        {
            return new Prompt()
                .Section(CorePrompt)
                .Section(ResponseFormattingRulesPrompt)
                .Section(CharacterPrompt)
                .Section(MemoryPrompt)
                .Section(InterNpcInteractionPrompt(takenInterNpcInteractions))
                .Section(SystemNotificationsPrompt(takenSystemNotifications))
                .Section(WorldStatePrompt)
                .Section(AnswerPrompt);
        }

        private string Time()
        {
            return Environment.Current == null ? "неизвестно" : Environment.Current.Clock.DateTime();
        }

        private string WorldState()
        {
            return "Игровое время: " + Time() + "\n" +
                    "Твоё состояние:\n" + Digestion.Of(this, DigestionDetail.Full) + "\n" +
                    "Объекты рядом с тобой:\n" + DigestNearObjects();
        }

        private string DigestNearObjects()
        {
            var digest = new StringBuilder();

            foreach (Component nearObject in FindNearObjects())
            {
                string seen = Digestion.Seen(nearObject, DigestionDetail.Brief, transform);
                if (seen != null) digest.AppendLine(seen);
            }

            return digest.ToString();
        }

        [SerializeField] private float nearObjectsScanRadius = 250f;
        private HashSet<Component> FindNearObjects()
        {
            var found = new HashSet<Component>();

            Collider[] hits = Physics.OverlapSphere(transform.position, nearObjectsScanRadius);

            foreach (Collider hit in hits)
            {
                if (!(hit.GetComponentInParent<IDigestible>() is Component owner)) continue;
                if (owner.gameObject == gameObject) continue;

                found.Add(owner);
            }

            return found;
        }

        private void Remember(string update)
        {
            if (update == null) return;

            memory = update.Length <= memoryLimit ? update : update.Substring(0, memoryLimit);
            Log.Info("Entity {} rewrote its memory ({} chars): {}", name, memory.Length, memory);
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

                foreach (Llm llm in allLlms)
                {
                    if (llm == this) continue;

                    string targetName = llm.PromptName();
                    if (targetName == null || !cmd.TargetNames.Contains(targetName)) continue;

                    received.Add(targetName);

                    Log.Info("Entity {} said to {}: {}", name, targetName, cmd.Content);

                    llm.interNpcInteractionInbox.Put("[" + Time() + "] " + ownName + ": " + cmd.Content);
                }

                foreach (string targetName in cmd.TargetNames)
                {
                    if (!received.Contains(targetName))
                    {
                        Log.Warn("Failed to said from {} to {}", name, targetName);
                        systemNotificationsInbox.Put("[" + Time() + "] " + $"Не удалось доставить твое сообщение до {targetName}: имя введено с ошибкой, цель не является llm npc или цель мертва");
                    }
                }
            }
        }

        private string PromptName()
        {
            return TryGetComponent(out Nameable nameable) ? nameable.PromptName() : null;
        }
    }
}
