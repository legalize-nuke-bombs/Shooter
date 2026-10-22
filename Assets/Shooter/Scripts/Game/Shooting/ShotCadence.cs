namespace Shooter.Game
{
    // The server fires by it and the shooter's machine kicks its camera by it, so the kick needs no round trip and still
    // follows the same shots
    public sealed class ShotCadence
    {
        private float lastShotAt = float.NegativeInfinity;
        private int shotIndexInSpray;

        public bool CanFire(FirearmAsset asset, bool sustained, float now)
        {
            if (sustained && asset.FireMode != FireMode.Auto) return false;

            return now - lastShotAt >= asset.FireInterval;
        }

        public int Fire(FirearmAsset asset, float now)
        {
            if (now - lastShotAt >= asset.SprayRecovery) shotIndexInSpray = 0;

            int shotIndex = shotIndexInSpray;
            shotIndexInSpray++;
            lastShotAt = now;
            return shotIndex;
        }
    }
}
