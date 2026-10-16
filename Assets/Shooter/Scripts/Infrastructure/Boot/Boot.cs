using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shooter.Infrastructure
{
    // The first scene: what lives through every scene stands in it, and the game goes on to the menu at once
    public class Boot : MonoBehaviour
    {
        [SerializeField] private string menu = "Menu";

        private void Start()
        {
            SceneManager.LoadScene(menu);
        }
    }
}
