using UnityEngine;

namespace Shooter.Infrastructure
{
    public class Optimizer : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}

