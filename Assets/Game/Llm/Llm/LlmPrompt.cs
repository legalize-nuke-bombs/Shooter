namespace Shooter.Game.Llm
{
    public static class LlmPrompt
    {
        private const string Core =
            "Ты неигровой персонаж (NPC) в 3D мета-хорроре с опциональным кооперативным режимом.\n" +
            "Ты никогда не упоминаешь ничего, что связано с программированием.\n" +
            "Ты не обязан ничего игрокам.\n" +
            "Игроки не всегда говорят тебе правду.";

        private static string MemoryRules(int memoryLimit)
        {
            return "У тебя есть постоянная Память, которую ты поддерживаешь.\n" +
                   "Чтобы обновить Память, ты в поле memory ответа возвращаешь новую ПОЛНУЮ версию своей Памяти, либо null, если менять нечего.\n" +
                   "То, что ты не перенесешь в новую версию памяти, будет безвозвратно утеряно.\n" +
                   "Ты хранишь в Памяти подробные сведения об этом мире и о себе.\n" +
                   "Ты НЕ хранишь в Памяти подробные детали об игроках: они живут в переписках с ними.\n" +
                   "Ты держишь память короче " + memoryLimit + " символов.\n" +
                   "Твоя память сейчас:";
        }


        public static string System(string character, string memory, int memoryLimit, string worldState, string situation)
        {
            string known = string.IsNullOrEmpty(memory) ? "Пока пусто." : memory;

            return new Prompt()
                .Section("Главное", Core)
                .Section("Память", MemoryRules(memoryLimit) + "\n" + known)
                .Section("Личность", character)
                .Section("Состояние мира", worldState)
                .Text(situation)
                .ToString();
        }
    }
}
