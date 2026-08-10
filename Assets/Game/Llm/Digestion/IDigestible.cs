namespace Shooter.Game.Llm
{
    public interface IDigestible
    {
        DigestionPriority Priority { get; }

        string Digest(DigestionDetail detail);
    }
}
