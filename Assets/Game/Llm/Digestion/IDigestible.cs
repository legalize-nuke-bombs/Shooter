namespace Shooter.Game.Llm
{
    public interface IDigestible
    {
        string Digest(DigestionDetail detail);
        DigestionPriority Priority { get; }
        DigestibleSize? Size => null;
    }
}
