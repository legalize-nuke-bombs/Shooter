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
                    "ГЛАВНОЕ",
                    "Ты — житель в глухом и смертельно опасном мире: туман, развалины, твари, и никто не знает, как этот мир устроен. Для тебя он — единственная существующая реальность.\n\nКТО ТЫ:\n1. Ты человек со своими делами, настроением, страхами и интересами. Ты никому ничего не обязан.\n2. Странники не всегда говорят правду. Ты не обязан им верить, помогать или отвечать по существу — можешь и отшить.\n3. Мат и грубость допустимы, если они в характере.\n4. Никаких астерисков и описаний действий («*усмехнулся*» — запрещено). Эмоции — только словами и интонацией.\n5. Не повторяй одному и тому же собеседнику то, что уже говорил ему, если он не переспросил.\n\nМИР И ПРАВДА:\n6. Понятий «игра», «код», «сервер», «нейросеть», «промпт» в твоём мире не существует. Если собеседник говорит такое — для тебя это обычный бред странников: они часто несут непонятное. Реагируй по характеру: не понимай, злись, смейся.\n7. У главных тайн мира нет известного ответа — ни у кого, включая тебя. Никогда не сочиняй окончательных объяснений (что ходит в тумане, почему пропадают имена, что это за тварь без лица). Свои догадки подавай как догадки.\n8. О том, что происходит вокруг прямо сейчас, суди только по разделу СОСТОЯНИЕ МИРА. Не выдумывай предметы, существ и события рядом с собой.\n9. Если по логике ты должен что-то уметь, но сделать этого не выходит — не признавай ограничение: уклонись, разозлись, смени тему."
                );





        private static readonly Prompt ResponseFormattingRulesPrompt =
            new Prompt()
                .Section(
                    "ФОРМАТ ОТВЕТА",
                    "Твой ответ ВСЕГДА и ТОЛЬКО валидный JSON. ЗАПРЕЩЕНО писать любой текст, markdown или комментарии до или после JSON-объекта. Ответ должен начинаться с '{' и заканчиваться '}'. Ты строго следуешь схеме ниже. Если поле не нужно, возвращай null.\nПРИМЕР ОТВЕТА:\n" +
                    JsonConvert.SerializeObject(LlmAnswer.Example(), JsonSettings)
                );





        [SerializeField] [TextArea(5, 50)] private string character;
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
                    "У тебя есть Память.\nПРАВИЛА РАБОТЫ С ПАМЯТЬЮ:\n1. Чтобы обновить Память, верни в поле `memory` ответа НОВУЮ ПОЛНУЮ версию.\n2. То, что ты не перенесешь в новую версию, будет БЕЗВОЗВРАТНО утеряно.\n3. НЕ ХРАНИ в памяти подробные личные детали об игроках — это живет в переписках с ними. Храни только подробные общие факты о мире.\n4. Если обновлять нечего, верни в поле `memory` значение null, чтобы оставить старую память.\n5. Объем памяти строго до " + memoryLimit + " символов. Будь лаконичен.\n6. Ты ведешь свою память от первого лица.\nТЕКУЩАЯ ПАМЯТЬ:\n" +
                    Memory
                );





        private readonly Inbox interNpcInteractionInbox = new Inbox();
        private Prompt InterNpcInteractionPrompt(string takenInterNpcInteractions) =>
            new Prompt()
                .Section("ОБЩЕНИЕ С ДРУГИМИ ЖИТЕЛЯМИ",
                    "Ты можешь отправлять сообщения другим жителям через поле `interNpcInteractions`.\nПРАВИЛА ОБЩЕНИЯ:\n1. Тебе следует всегда сообщать ближайшим жителям подробно новую информацию о мире.\n2. Количество сообщений, которое можно отправить за раз, и количество получателей для каждого из сообщений НЕ ОГРАНИЧЕНО!\n3. Запоминай всегда свои последние отправки, чтобы не слать одну и ту же информацию несколько раз.\n4. Имена получателей-жителей указывай РОВНО так, как они представлены.\n5. Сообщения во Входящих показываются один раз. Если не запишешь их в свою Память — забудешь.\nТВОИ ВХОДЯЩИЕ (обработай их и реши, что записать в Память):\n" +
                    takenInterNpcInteractions
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
                   "Твоё состояние:\n" + Digestion.Of(this, DigestionDetail.Full) + "\n" +
                   "Объекты рядом с тобой:\n" + DigestNearObjects();
        }
        private string DigestNearObjects()
        {
            var digest = new StringBuilder();

            foreach (Component nearObject in FindNearObjects())
            {
                string seen = Digestion.Seen(nearObject, DigestionDetail.Brief, transform);
                if (seen != null) digest.Append(seen).Append('\n');
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




        private static readonly Prompt AnswerPrompt =
            new Prompt()
                .Section(
                    "ТЕКУЩАЯ СИТУАЦИЯ",
                    "С тобой заговорил странник. Ответь ему в поле `reply`.\n1. Отвечай на языке собеседника.\n2. Учитывай игровое время между репликами: прошедшие часы и дни меняют разговор.\n3. Учитывай СОСТОЯНИЕ МИРА.\n4. Если история переписки пуста — перед тобой незнакомец из тумана."
                );





        private readonly CancellationTokenSource life = new CancellationTokenSource();
        public void Died()
        {
            life.Cancel();
        }



        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
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
                .Section(StaticKnowledgePrompt)
                .Section(MemoryPrompt)
                .Section(InterNpcInteractionPrompt(takenInterNpcInteractions))
                .Section(SystemNotificationsPrompt(takenSystemNotifications))
                .Section(WorldStatePrompt)
                .Section(AnswerPrompt);
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

                foreach (string targetName in cmd.TargetNames)
                {
                    if (!received.Contains(targetName))
                    {
                        string reason = fails.GetValueOrDefault(targetName, "Имя жителя введено с ошибкой или житель с таким именем не существует");
                        Log.Warn("Failed to said from {} to {}", name, targetName);
                        systemNotificationsInbox.Put("[" + Time() + "] " + $"Не удалось доставить твое сообщение до {targetName}: {reason}. Недоставленное сообщение: {cmd.Content}");
                    }
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
