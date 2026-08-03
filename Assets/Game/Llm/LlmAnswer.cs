namespace Shooter.Game.Llm
{
    public class LlmAnswer
    {
        public string Reply { get; set; }
        public string Memory { get; set; }
        public LlmInterNpcInteractionCommand[] InterNpcInteractions { get; set; }

        public class LlmInterNpcInteractionCommand
        {
            public string[] TargetNames { get; set; }
            public string Content { get; set; }
        }

        public static LlmAnswer Example()
        {
            return new LlmAnswer()
            {
                Reply = "Твой ответ игроку",
                Memory = "Твоя обновленная Память ЦЕЛИКОМ",
                InterNpcInteractions = new LlmInterNpcInteractionCommand[]
                {
                    new LlmInterNpcInteractionCommand()
                    {
                        TargetNames = new string[] { "Первый получатель первого сообщения", "Второй получатель первого сообщения" },
                        Content = "Содержимое первого сообщения"
                    },
                    new LlmInterNpcInteractionCommand()
                    {
                        TargetNames = new string[] { "Первый получатель второго сообщения", "Второй получатель второго сообщения" },
                        Content = "Содержимое второго сообщения"
                    },
                }
            };
        }
    }

}
