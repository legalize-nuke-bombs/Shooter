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
                Reply = "Твой ответ игроку (только если ты с ним сейчас говоришь)",
                Memory = "Твоя обновленная Память ЦЕЛИКОМ (только если стало известное что-то новое)",
                InterNpcInteractions = new LlmInterNpcInteractionCommand[]
                {
                    new LlmInterNpcInteractionCommand()
                    {
                        TargetNames = new string[] { "Первый получатель твоего первого сообщения", "Второй получатель твоего первого сообщения" },
                        Content = "Содержимое твоего первого сообщения"
                    },
                    new LlmInterNpcInteractionCommand()
                    {
                        TargetNames = new string[] { "Первый получатель твоего второго сообщения", "Второй получатель твоего второго сообщения" },
                        Content = "Содержимое твоего второго сообщения"
                    },
                }
            };
        }
    }

}
