namespace Shooter.Game.Body.Hitboxes
{
    public static class BodyParts
    {
        public static float Multiplier(this BodyPart part)
        {
            return part switch
            {
                BodyPart.Head => 4f,
                BodyPart.Limbs => 0.75f,
                _ => 1f
            };
        }
    }
}
