namespace Shooter.Game.Core
{
    public interface IDigestible
    {
        DigestionPriority Priority { get; }
        string Digest(DigestionDetail detail);
    }
}
