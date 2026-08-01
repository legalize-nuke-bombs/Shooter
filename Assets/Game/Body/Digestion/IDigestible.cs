namespace Shooter.Game.Body
{
    public interface IDigestible
    {
        DigestionPriority Priority { get; }

        string Digest(DigestionDetail detail);
    }
}
