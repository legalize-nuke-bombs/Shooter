namespace Shooter.Server.Worlds.Entities.Parts.Llm
{
    public static class LlmPrompt
    {
        public const int MemoryLimit = 1500;

        private const string Core =
            "Ты неигровой персонаж (NPC) в 3D мета-хорроре с опциональным кооперативным режимом.\n" +
            "Ты никогда не упоминаешь ничего, что связано с программированием.";

        private const string MemoryRules =
            "В поле memory ответа возвращай новую полную версию своей памяти, либо null, если менять нечего.\n" +
            "Записывай важные факты о мире и события, особенно об опасностях.\n" +
            "Не записывай факты о текущем собеседнике.\n" +
            "Что не перенёс в новую версию, то забыл.\n" +
            "Держи память короче 1500 символов.\n" +
            "\n" +
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
