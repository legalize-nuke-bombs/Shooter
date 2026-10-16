namespace Shooter.Game
{
    public interface IDigestible
    {
        DigestionPriority Priority { get; }
        string Digest(DigestionDetail detail);
    }
}
