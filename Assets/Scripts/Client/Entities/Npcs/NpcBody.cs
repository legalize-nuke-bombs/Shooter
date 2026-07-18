using UnityEngine;

namespace Shooter.Client.Entities.Npcs
{
    public class NpcBody : MonoBehaviour
    {
        public NpcAvatar Avatar { get; private set; }

        public static void Attach(GameObject body, NpcAvatar avatar)
        {
            body.AddComponent<NpcBody>().Avatar = avatar;
        }
    }
}
