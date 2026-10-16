using UnityEngine;

namespace Shooter.Infrastructure
{
    // What the menu asks of the world it starts, and why the world sent the player back.
    // The menu and the map both point at this one asset; nothing here is saved into it
    [CreateAssetMenu(menuName = "Shooter/Launch", fileName = "Launch")]
    public class Launch : ScriptableObject
    {
        public string Save { get; set; }

        public Refusal Ending { get; set; }
    }
}
