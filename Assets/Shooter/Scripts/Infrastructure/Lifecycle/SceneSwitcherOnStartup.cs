using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    [DisallowMultipleComponent]
    public class SceneSwitcherOnStartup : ShooterComponent
    {
        [SerializeField] private string scene;

        private void Start()
        {
            SceneManager.LoadScene(scene);
        }
    }
}
