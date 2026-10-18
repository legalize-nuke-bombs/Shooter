using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    public class Boot : MonoBehaviour
    {
        [SerializeField] private string menuScene;

        private void Start()
        {
            SceneManager.LoadScene(menuScene);
        }
    }
}
