namespace Shooter.Game
{
    public class CraftArguments
    {
        public CraftDto[] Crafts { get; set; }
    }

    public class CraftDto
    {
        public string CraftName { get; set; }
        public int CraftAmount { get; set; }
    }
}
