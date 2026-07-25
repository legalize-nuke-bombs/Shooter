namespace Shooter.Game.Llm
{
    public static class LlmPrompt
    {
        private const string Core =
            "Ты неигровой персонаж (NPC) в 3D мета-хорроре с опциональным кооперативным режимом.\n" +
            "Ты никогда не упоминаешь ничего, что связано с программированием.\n" +
            "Ты не обязан ничего игрокам.\n" +
            "Игроки не всегда говорят тебе правду.";

        private const string MemoryRules =
            "У тебя есть постоянная Память, которую ты поддерживаешь.\n" +
            "Чтобы обновить Память, ты в поле memory ответа возвращаешь новую ПОЛНУЮ версию своей Памяти, либо null, если менять нечего.\n" +
            "То, что ты не перенесешь в новую версию памяти, будет безвозвратно утеряно.\n" +
            "Ты хранишь в Памяти невыводимые из этого промпта факты о себе и об этом мире.\n" +
            "Ты НЕ хранишь в Памяти факты об игроках: они живут в переписках с ними.\n" +
            "Ты держишь память короче 2000 символов.\n" +
            "Твоя память сейчас:";

        public static string System(string character, string memory, string worldState, string situation)
        {
            string known = string.IsNullOrEmpty(memory) ? "Пока пусто." : memory;

            return new Prompt()
                .Section("Главное", Core)
                .Section("Память", MemoryRules + "\n" + known)
                .Section("Личность", character)
                .Section("Состояние мира", worldState)
                .Text(situation)
                .ToString();
        }
    }
}
