using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    public class SceneSwitcherOnStartup : MonoBehaviour
    {
        [SerializeField] private string scene;

        private void Start()
        {
            SceneManager.LoadScene(scene);
        }
    }
}
