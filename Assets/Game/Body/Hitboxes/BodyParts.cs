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

        public static float Generosity(this BodyPart part)
        {
            return part switch
            {
                BodyPart.Head => 1.25f,
                BodyPart.Limbs => 1.2f,
                _ => 1.15f
            };
        }
    }
}
