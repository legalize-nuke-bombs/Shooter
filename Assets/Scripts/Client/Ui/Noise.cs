namespace Shooter.Client.Ui
{
    public static class Noise
    {
        public static int Hash(int a, int b)
        {
            int hash = (a * 73856093) ^ (b * 19349663);
            return (hash >> 13) ^ hash;
        }

        public static float Wrap(float value, float range)
        {
            float wrapped = value % range;
            return wrapped < 0f ? wrapped + range : wrapped;
        }
    }
}
