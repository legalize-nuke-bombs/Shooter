namespace Shooter.Game.Core
{
    public interface IDigestible
    {
        string Digest(DigestionDetail detail);

        DigestionPriority Priority { get; }
    }
}
