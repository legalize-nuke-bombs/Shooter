namespace Shooter.Infrastructure
{
    public static class ExecutionOrder
    {
        public const int InfrastructureSystemCore = -1000;
        public const int InfrastructureEntity = -900;
        public const int InfrastructureSystemExtension = -800;

        public const int Service = -100;
    }
}
